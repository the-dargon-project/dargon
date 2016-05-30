using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.Pooling;
using Dargon.Courier.TransportTier;
using Nito.AsyncEx;
using NLog;

namespace Dargon.Courier.TransitTier {
   public class UdpTransport : ITransport {
      public const int kMaximumTransportSize = 8192;
      private const int kPort = 21337;
      private static readonly IPAddress kMulticastAddress = IPAddress.Parse("235.13.33.37");
      private static readonly IPEndPoint kSendEndpoint = new IPEndPoint(kMulticastAddress, kPort);
      private static readonly IPEndPoint kReceiveEndpoint = new IPEndPoint(IPAddress.Any, kPort);
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      private readonly List<Socket> sockets;
      private readonly IObjectPool<InboundDataEvent> inboundDataEventPool; 
      private readonly IObjectPool<AsyncAutoResetEvent> asyncAutoResetEventPool; 
      private readonly IObjectPool<SocketAsyncEventArgs> sendArgsPool; 
      private readonly IObjectPool<SocketAsyncEventArgs> receiveArgsPool;
      private IAsyncPoster<InboundDataEvent> inboundDataEventPoster;
      private IAsyncSubscriber<MemoryStream> outboundDataSubscriber;

      private UdpTransport(List<Socket> sockets) {
         this.sockets = sockets;
         this.inboundDataEventPool = ObjectPool.Create(() => new InboundDataEvent());
         this.asyncAutoResetEventPool = ObjectPool.Create(() => new AsyncAutoResetEvent());
         this.sendArgsPool = ObjectPool.Create(() => new SocketAsyncEventArgs());
         this.receiveArgsPool = ObjectPool.Create(() => {
            return new SocketAsyncEventArgs {
               RemoteEndPoint = kReceiveEndpoint
            }.With(x => {
               x.SetBuffer(new byte[kMaximumTransportSize], 0, kMaximumTransportSize);
               x.Completed += HandleReceiveCompleted;
            });
         });
      }

      public void Start(IAsyncPoster<InboundDataEvent> inboundDataEventPoster, IAsyncSubscriber<MemoryStream> outboundDataSubscriber) {
         this.inboundDataEventPoster = inboundDataEventPoster;
         this.outboundDataSubscriber = outboundDataSubscriber;

         sockets.ForEach(BeginReceive);
         outboundDataSubscriber.Subscribe(HandleOutboundDataSendAsync);
      }

      private void BeginReceive(Socket socket) {
         var e = receiveArgsPool.TakeObject();
         e.AcceptSocket = socket;
         socket.ReceiveFromAsync(e);
      }

      private void HandleReceiveCompleted(object sender, SocketAsyncEventArgs e) {
         BeginReceive(e.AcceptSocket);
         HandleReceiveCompletedHelperAsync(e).Forget();
      }

      private async Task HandleReceiveCompletedHelperAsync(SocketAsyncEventArgs e) {
         logger.Error($"Received from {e.RemoteEndPoint} {e.BytesTransferred} bytes!");
         var inboundDataEvent = inboundDataEventPool.TakeObject();
         inboundDataEvent.Data = e.Buffer;
         await inboundDataEventPoster.PostAsync(inboundDataEvent);
         receiveArgsPool.ReturnObject(e);
         inboundDataEventPool.ReturnObject(inboundDataEvent);
      }

      public Task HandleOutboundDataSendAsync(IAsyncSubscriber<MemoryStream> subscriber, MemoryStream payload) {
         logger.Error($"Sending {payload.Length} bytes!");
         var sync = asyncAutoResetEventPool.TakeObject();
         foreach (var socket in sockets) {
            var e = sendArgsPool.TakeObject();
            e.RemoteEndPoint = kSendEndpoint;
            e.SetBuffer(payload.GetBuffer(), 0, (int)payload.Length);
            e.Completed += (sender, args) => {
               e.Dispose();
               sync.Set();
            };
            if (!socket.SendToAsync(e)) {
               // Completed synchronously. e.Completed won't be called.
               sendArgsPool.ReturnObject(e);
               sync.Set();
            }
         }
         return sync.WaitAsync();
      }

      public static UdpTransport Create() {
         var sockets = new List<Socket>();
         foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces()) {
            if (!networkInterface.SupportsMulticast ||
                networkInterface.OperationalStatus != OperationalStatus.Up ||
                networkInterface.IsReceiveOnly) continue;
            var ipv4Properties = networkInterface.GetIPProperties()?.GetIPv4Properties();
            if (ipv4Properties != null)
               sockets.Add(CreateSocket(ipv4Properties.Index));
         }
         return new UdpTransport(sockets);
      }

      private static Socket CreateSocket(long adapterIndex) {
         var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) {
            DontFragment = false,
            MulticastLoopback = true
         };
         socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
         socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(kMulticastAddress));
         socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1); //0: localhost, 1: lan (via switch)
         socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, (int)IPAddress.HostToNetworkOrder(adapterIndex));
         socket.Bind(new IPEndPoint(IPAddress.Any, kPort));
         return socket;
      }

      public void Dispose() {
         foreach (var socket in sockets) {
            socket.Dispose();
         }
      }
   }
}
