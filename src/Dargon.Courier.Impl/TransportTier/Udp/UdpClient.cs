using System;
using Dargon.Commons;
using Dargon.Commons.Pooling;
using Nito.AsyncEx;
using NLog;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Dargon.Courier.AuditingTier;

namespace Dargon.Courier.TransportTier.Udp {
   public class UdpClient {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      private readonly IObjectPool<InboundDataEvent> inboundSomethingEventPool = ObjectPool.Create(() => new InboundDataEvent());
      private readonly IObjectPool<AsyncAutoResetEvent> asyncAutoResetEventPool = ObjectPool.Create(() => new AsyncAutoResetEvent());
      private readonly IObjectPool<SocketAsyncEventArgs> sendArgsPool = ObjectPool.Create(() => new SocketAsyncEventArgs());
      
      private readonly UdpTransportConfiguration configuration;
      private readonly List<Socket> sockets;
      private readonly AuditAggregator<double> inboundBytesAggregator;
      private readonly AuditAggregator<double> outboundBytesAggregator;

      private readonly IObjectPool<SocketAsyncEventArgs> receiveArgsPool;

      private volatile bool isShutdown = false;
      private UdpDispatcher udpDispatcher;

      private UdpClient(UdpTransportConfiguration configuration, List<Socket> sockets, AuditAggregator<double> inboundBytesAggregator, AuditAggregator<double> outboundBytesAggregator) {
         this.configuration = configuration;
         this.sockets = sockets;
         this.inboundBytesAggregator = inboundBytesAggregator;
         this.outboundBytesAggregator = outboundBytesAggregator;
         this.receiveArgsPool = ObjectPool.Create(() => {
            return new SocketAsyncEventArgs {
               RemoteEndPoint = configuration.ReceiveEndpoint
            }.With(x => {
               x.SetBuffer(new byte[UdpConstants.kMaximumTransportSize], 0, UdpConstants.kMaximumTransportSize);
               x.Completed += HandleReceiveCompleted;
            });
         });
      }

      public void StartReceiving(UdpDispatcher udpDispatcher) {
         this.udpDispatcher = udpDispatcher;
         sockets.ForEach(BeginReceive);
      }

      private void BeginReceive(Socket socket) {
         var e = receiveArgsPool.TakeObject();
         e.AcceptSocket = socket;

         try {
            socket.ReceiveFromAsync(e);
         } catch (ObjectDisposedException) when (isShutdown) {
            // socket was probably shut down
         }
      }

      private void HandleReceiveCompleted(object sender, SocketAsyncEventArgs e) {
         BeginReceive(e.AcceptSocket);
         HandleReceiveCompletedHelperAsync(e).Forget();
      }

      private async Task HandleReceiveCompletedHelperAsync(SocketAsyncEventArgs e) {
//         logger.Debug($"Received from {e.RemoteEndPoint} {e.BytesTransferred} bytes!");
         var inboundSomethingEvent = inboundSomethingEventPool.TakeObject();
         inboundSomethingEvent.Data = e.Buffer;

         await udpDispatcher.HandleInboundDataEventAsync(inboundSomethingEvent).ConfigureAwait(false);

         receiveArgsPool.ReturnObject(e);
         inboundSomethingEventPool.ReturnObject(inboundSomethingEvent);

         // analytics
         inboundBytesAggregator.Put(e.BytesTransferred);
      }

      public async Task BroadcastAsync(MemoryStream ms, int offset, int length) {
//         logger.Debug($"Sending {length} bytes!");
         var sync = asyncAutoResetEventPool.TakeObject();
         foreach (var socket in sockets) {
            var e = sendArgsPool.TakeObject();
            e.RemoteEndPoint = configuration.SendEndpoint;
            e.SetBuffer(ms.GetBuffer(), 0, length);
            e.Completed += (sender, args) => {
               e.Dispose();
               sync.Set();
            };
            try {
               if (!socket.SendToAsync(e)) {
                  // Completed synchronously. e.Completed won't be called.
                  sendArgsPool.ReturnObject(e);
                  sync.Set();
               }
            } catch (ObjectDisposedException) when (isShutdown) { }
         }
         await sync.WaitAsync();

         // analytics
         outboundBytesAggregator.Put(length);
      }

      public void Shutdown() {
         isShutdown = true;
         foreach (var socket in sockets) {
            socket.Close();
            socket.Dispose();
         }
      }

      public static UdpClient Create(UdpTransportConfiguration udpTransportConfiguration, AuditAggregator<double> inboundBytesAggregator, AuditAggregator<double> outboundBytesAggregator) {
         var sockets = new List<Socket>();
         foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces()) {
            if (!networkInterface.SupportsMulticast ||
                networkInterface.OperationalStatus != OperationalStatus.Up ||
                networkInterface.IsReceiveOnly) continue;
            var ipv4Properties = networkInterface.GetIPProperties()?.GetIPv4Properties();
            if (ipv4Properties != null)
               sockets.Add(CreateSocket(ipv4Properties.Index, udpTransportConfiguration));
         }
         return new UdpClient(udpTransportConfiguration, sockets, inboundBytesAggregator, outboundBytesAggregator);
      }

      private static Socket CreateSocket(long adapterIndex, UdpTransportConfiguration udpTransportConfiguration) {
         var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) {
            DontFragment = false,
            MulticastLoopback = true
         };
         socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
         socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(udpTransportConfiguration.MulticastAddress));
         socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1); //0: localhost, 1: lan (via switch)
         socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, (int)IPAddress.HostToNetworkOrder(adapterIndex));
         socket.Bind(new IPEndPoint(IPAddress.Any, udpTransportConfiguration.ReceiveEndpoint.Port));
         return socket;
      }
   }
}
