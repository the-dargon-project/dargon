using Dargon.Commons;
using Dargon.Commons.Pooling;
using Dargon.Vox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dargon.Courier {
   public class UdpTransport : IDisposable {
      private const int kMaximumTransportSize = 8192;
      private const int kPort = 21337;
      private static readonly IPAddress kMulticastAddress = IPAddress.Parse("235.13.33.37");
      private static readonly IPEndPoint kSendEndpoint = new IPEndPoint(kMulticastAddress, kPort);
      private static readonly IPEndPoint kReceiveEndpoint = new IPEndPoint(IPAddress.Any, kPort);

      private readonly List<Socket> sockets;
      private readonly IAsyncConsumer<object> receiver;
      private readonly IAsyncProducer<object> outboundConsumable;
      private readonly IObjectPool<MemoryStream> sendBufferPool; 
      private readonly IObjectPool<SocketAsyncEventArgs> sendArgsPool; 
      private readonly IObjectPool<SocketAsyncEventArgs> receiveArgsPool;

      private UdpTransport(List<Socket> sockets, IAsyncConsumer<object> receiver, IAsyncProducer<object> outboundConsumable) {
         this.sockets = sockets;
         this.receiver = receiver;
         this.outboundConsumable = outboundConsumable;
         this.sendBufferPool = ObjectPool.Create(() => new MemoryStream());
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

      public void Initialize() {
         sockets.ForEach(BeginReceive);
         outboundConsumable.Subscribe(HandleOutboundSendAsync);
      }

      private void BeginReceive(Socket socket) {
         var e = receiveArgsPool.TakeObject();
         e.AcceptSocket = socket;
      }

      private void HandleReceiveCompleted(object sender, SocketAsyncEventArgs e) {
         BeginReceive(e.AcceptSocket);
         HandleReceiveCompletedHelperAsync(e).Forget();
      }

      private async Task HandleReceiveCompletedHelperAsync(SocketAsyncEventArgs e) {
         var message = Deserialize.From(e.Buffer, 0, e.BytesTransferred);
         await receiver.PostAsync(message);
         receiveArgsPool.ReturnObject(e);
      }

      private async Task HandleOutboundSendAsync(IAsyncProducer<object> producer, object payload) {
         await Task.Yield();

         var ms = sendBufferPool.TakeObject();
         Serialize.To(ms, payload);

         HandleOutboundSendAsyncHelperAsync(ms).Forget();
      }

      private async Task HandleOutboundSendAsyncHelperAsync(MemoryStream ms) {
         await Task.Yield();

         foreach (var socket in sockets) {
            var e = sendArgsPool.TakeObject();
            e.RemoteEndPoint = kSendEndpoint;
            e.SetBuffer(ms.GetBuffer(), 0, (int)ms.Length);
            e.Completed += (sender, args) => {
               e.Dispose();
               sendBufferPool.ReturnObject(ms);
            };
            if (!socket.SendToAsync(e)) {
               sendArgsPool.ReturnObject(e);
               sendBufferPool.ReturnObject(ms);
            }
         }
      }

      public static UdpTransport Create(IAsyncConsumer<object> inboundPublisher, IAsyncProducer<object> outboundConsumable) {
         var sockets = new List<Socket>();
         foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces()) {
            if (!networkInterface.SupportsMulticast ||
                networkInterface.OperationalStatus != OperationalStatus.Up ||
                networkInterface.IsReceiveOnly) continue;
            var ipv4Properties = networkInterface.GetIPProperties()?.GetIPv4Properties();
            if (ipv4Properties != null)
               sockets.Add(CreateSocket(ipv4Properties.Index));
         }
         var transport = new UdpTransport(sockets, inboundPublisher, outboundConsumable);
         transport.Initialize();
         return transport;
      }

      private static Socket CreateSocket(long adapterIndex) {
         var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) {
            DontFragment = false,
            MulticastLoopback = true
         };
         socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
         socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(kMulticastAddress));
         socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1); //0: localhost, 1: lan (via switch)
         socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, IPAddress.HostToNetworkOrder(adapterIndex));
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
