using System;
using Dargon.Commons;
using Dargon.Commons.Pooling;
using Nito.AsyncEx;
using NLog;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Dargon.Courier.AuditingTier;

namespace Dargon.Courier.TransportTier.Udp {
   public class UdpClient {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      private readonly IObjectPool<InboundDataEvent> inboundSomethingEventPool = ObjectPool.CreateStackBacked(() => new InboundDataEvent());
      private readonly IObjectPool<AsyncAutoResetEvent> asyncAutoResetEventPool = ObjectPool.CreateStackBacked(() => new AsyncAutoResetEvent());
      
      private readonly UdpTransportConfiguration configuration;
      private readonly List<Socket> sockets;
      private readonly AuditAggregator<double> inboundBytesAggregator;
      private readonly AuditAggregator<double> outboundBytesAggregator;
      private readonly AuditAggregator<double> inboundReceiveProcessDispatchLatencyAggregator;

      private readonly IObjectPool<SocketAsyncEventArgs> sendArgsPool;
      private readonly IObjectPool<SocketAsyncEventArgs> receiveArgsPool;

      private volatile bool isShutdown = false;
      private UdpDispatcher udpDispatcher;

      private static int i = 0;

      private UdpClient(UdpTransportConfiguration configuration, List<Socket> sockets, AuditAggregator<double> inboundBytesAggregator, AuditAggregator<double> outboundBytesAggregator, AuditAggregator<double> inboundReceiveProcessDispatchLatencyAggregator) {
         this.configuration = configuration;
         this.sockets = sockets;
         this.inboundBytesAggregator = inboundBytesAggregator;
         this.outboundBytesAggregator = outboundBytesAggregator;
         this.inboundReceiveProcessDispatchLatencyAggregator = inboundReceiveProcessDispatchLatencyAggregator;
         this.sendArgsPool = ObjectPool.CreateStackBacked(() => new SocketAsyncEventArgs());
         this.receiveArgsPool = ObjectPool.CreateStackBacked(() => {
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
         HandleReceiveCompletedHelper(e);
      }

      private void HandleReceiveCompletedHelper(SocketAsyncEventArgs e) {
         // logger.Debug($"Received from {e.RemoteEndPoint} {e.BytesTransferred} bytes!");
         var sw = new Stopwatch();
         sw.Start();

         var inboundSomethingEvent = inboundSomethingEventPool.TakeObject();
         inboundSomethingEvent.Data = e.Buffer;

         udpDispatcher.HandleInboundDataEvent(inboundSomethingEvent);

         receiveArgsPool.ReturnObject(e);
         inboundSomethingEventPool.ReturnObject(inboundSomethingEvent);

         // analytics
         inboundBytesAggregator.Put(e.BytesTransferred);
         inboundReceiveProcessDispatchLatencyAggregator.Put(sw.ElapsedMilliseconds);
      }

      public async Task BroadcastAsync(MemoryStream ms, int offset, int length) {
//         logger.Debug($"Sending {length} bytes!");
         var sync = asyncAutoResetEventPool.TakeObject();
         foreach (var socket in sockets) {
            var e = sendArgsPool.TakeObject();
            e.RemoteEndPoint = configuration.SendEndpoint;
            e.SetBuffer(ms.GetBuffer(), 0, length);
            e.Completed += (sender, args) => {
               e.SetBuffer(null, 0, 0);
               e.Dispose();
               sync.Set();
            };
            try {
               if (!socket.SendToAsync(e)) {
                  // Completed synchronously. e.Completed won't be called.
                  // pooling was leading to leaks?
                  e.SetBuffer(null, 0, 0);
                  e.Dispose();
                  sync.Set();
               }
            } catch (ObjectDisposedException) when (isShutdown) { }
         }
         await sync.WaitAsync().ConfigureAwait(false);

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

      public static UdpClient Create(UdpTransportConfiguration udpTransportConfiguration, AuditAggregator<double> inboundBytesAggregator, AuditAggregator<double> outboundBytesAggregator, AuditAggregator<double> inboundReceiveProcessDispatchLatencyAggregator) {
         var sockets = new List<Socket>();
         foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces()) {
            if (!networkInterface.SupportsMulticast ||
                networkInterface.OperationalStatus != OperationalStatus.Up ||
                networkInterface.IsReceiveOnly) continue;
            var ipv4Properties = networkInterface.GetIPProperties()?.GetIPv4Properties();
            if (ipv4Properties != null)
               sockets.Add(CreateSocket(ipv4Properties.Index, udpTransportConfiguration));

            var ni = networkInterface;
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet) {
               Console.WriteLine(ni.Name);
               foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses) {
                  if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                     Console.WriteLine(ip.Address.ToString());
                  }
               }
            }
         }
         return new UdpClient(udpTransportConfiguration, sockets, inboundBytesAggregator, outboundBytesAggregator, inboundReceiveProcessDispatchLatencyAggregator);
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
