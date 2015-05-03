using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.Identities;
using ItzWarty.Collections;
using ItzWarty.Networking;
using ItzWarty.Pooling;

namespace Dargon.Courier.Networking {
   public class UdpCourierNetwork : CourierNetwork {
      private readonly INetworkingProxy networkingProxy;
      private readonly UdpCourierNetworkConfiguration configuration;

      public UdpCourierNetwork(INetworkingProxy networkingProxy, UdpCourierNetworkConfiguration configuration) {
         this.networkingProxy = networkingProxy;
         this.configuration = configuration;
      }

      public CourierNetworkContext Join(ReadableCourierEndpoint endpoint) {
         var context = new NetworkContextImpl(this, networkingProxy, configuration, endpoint);
         context.Initialize();
         return context;
      }

      private class NetworkContextImpl : CourierNetworkContext {
         private const int kMaximumTransportSize = 8192;
         private static readonly IPAddress kMulticastAddress = IPAddress.Parse("235.13.33.37");
         private readonly UdpCourierNetwork network;
         private readonly INetworkingProxy networkingProxy;
         private readonly UdpCourierNetworkConfiguration configuration;
         private readonly ReadableCourierEndpoint endpoint;
         private IPEndPoint multicastEndpoint;
         private Socket socket;
         private ObjectPool<ReceiveState> receiveStatePool;

         public event DataArrivedHandler DataArrived;

         public NetworkContextImpl(UdpCourierNetwork network, INetworkingProxy networkingProxy, UdpCourierNetworkConfiguration configuration, ReadableCourierEndpoint endpoint) {
            this.network = network;
            this.networkingProxy = networkingProxy;
            this.configuration = configuration;
            this.endpoint = endpoint;
         }

         public void Initialize() {
            multicastEndpoint = new IPEndPoint(kMulticastAddress, configuration.Port);

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // socket.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.NoDelay, 1);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            socket.Bind(new IPEndPoint(IPAddress.Any, configuration.Port));

            socket.DontFragment = false;
            socket.EnableBroadcast = true;
            socket.MulticastLoopback = true; // necessary to test locally
            var multicastOption = new MulticastOption(kMulticastAddress);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multicastOption);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1); //0 lan, 1 single router hop, 2 two router hops

            receiveStatePool = new ObjectPoolImpl<ReceiveState>(() => new ReceiveState(this, socket, new IPEndPoint(IPAddress.Any, configuration.Port)));

            BeginNextReceive();
         }

         private void BeginNextReceive() {
            var receiveState = receiveStatePool.TakeObject();
            receiveState.BeginReceive();
         }

         private void HandleReceiveCompleted(ReceiveState receiveState, byte[] buffer, int i, int bytesTransferred) {
            var capture = DataArrived;
            if (capture != null) {
               capture.BeginInvoke(network, buffer, ar => { receiveStatePool.ReturnObject(receiveState); }, null);
            }
         }

         public void Broadcast(byte[] payload) {
            Broadcast(payload, 0, payload.Length);
         }

         public void Broadcast(byte[] payload, int offset, int length) {
            var e = new SocketAsyncEventArgs();
            e.SetBuffer(payload, offset, length);
            e.RemoteEndPoint = multicastEndpoint;
            socket.SendToAsync(e);
         }

         internal class ReceiveState {
            private readonly byte[] buffer = new byte[kMaximumTransportSize];
            private readonly NetworkContextImpl networkContext;
            private readonly Socket socket;
            private readonly SocketAsyncEventArgs socketEventArgs;

            public ReceiveState(NetworkContextImpl networkContext, Socket socket, IPEndPoint remoteEndPoint) {
               this.networkContext = networkContext;
               this.socket = socket;

               this.socketEventArgs = new SocketAsyncEventArgs();
               this.socketEventArgs.AcceptSocket = socket;
               this.socketEventArgs.UserToken = this;
               this.socketEventArgs.RemoteEndPoint = remoteEndPoint;
               this.socketEventArgs.SetBuffer(buffer, 0, buffer.Length);
               this.socketEventArgs.Completed += HandleReceiveCompleted;
            }

            public void BeginReceive() {
               socket.ReceiveFromAsync(socketEventArgs);
            }

            public void HandleReceiveCompleted(object sender, SocketAsyncEventArgs e) {
               networkContext.BeginNextReceive();
               networkContext.HandleReceiveCompleted(this, buffer, 0, e.BytesTransferred);
            }
         }
      }
   }

   public class UdpCourierNetworkConfiguration {
      public UdpCourierNetworkConfiguration(int port) {
         Port = port;
      }

      public int Port { get; }
   }
}
