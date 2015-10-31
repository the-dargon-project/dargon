using System;
using System.Collections.Generic;
using System.Data.Common;
using Dargon.Courier.Identities;
using ItzWarty.Networking;
using ItzWarty.Pooling;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using ItzWarty;
using NLog;

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

         private static readonly Logger logger = LogManager.GetCurrentClassLogger();
         private static readonly IPAddress kMulticastAddress = IPAddress.Parse("235.13.33.37");

         private readonly UdpCourierNetwork network;
         private readonly INetworkingProxy networkingProxy;
         private readonly UdpCourierNetworkConfiguration configuration;
         private readonly ReadableCourierEndpoint endpoint;
         private IPEndPoint multicastEndpoint;
         private Socket socket;
         private ObjectPool<ReceiveState> receiveStatePool;
         private readonly ObjectPool<SocketAsyncEventArgs> broadcastSocketAsyncEventArgsPool = new ObjectPoolImpl<SocketAsyncEventArgs>(() => new SocketAsyncEventArgs());

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
            socket.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.NoDelay, 1);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            socket.Bind(new IPEndPoint(IPAddress.Any, configuration.Port));

            socket.DontFragment = false;
            socket.EnableBroadcast = true;
            socket.MulticastLoopback = true; // necessary to test locally
            var multicastOption = new MulticastOption(kMulticastAddress);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multicastOption);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1); //0: localhost, 1: lan (via switch), 2: 1 mitm, etc...

            var bestNetworkInterface = NetworkInterface.GetAllNetworkInterfaces().MaxBy(RateOutboundNetworkInterfaceCandidate);
            var ipv4Properties = bestNetworkInterface?.GetIPProperties()?.GetIPv4Properties();
            if (ipv4Properties != null) {
               logger.Info("Selected best network interface: " + bestNetworkInterface.Name);
               socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, (int)IPAddress.HostToNetworkOrder(ipv4Properties.Index));
            }

            receiveStatePool = new ObjectPoolImpl<ReceiveState>(() => new ReceiveState(this, socket, new IPEndPoint(IPAddress.Any, configuration.Port)));

            BeginNextReceive();
         }
         
         private static int RateOutboundNetworkInterfaceCandidate(NetworkInterface networkInterface) {
            int rating = 0;
            if (!networkInterface.SupportsMulticast ||
                networkInterface.OperationalStatus != OperationalStatus.Up ||
                networkInterface.IsReceiveOnly) {
               rating = int.MinValue;
            } else {
               var ipProperties = networkInterface.GetIPProperties();
               var ipv4Properties = ipProperties.GetIPv4Properties();
               if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback) {
                  rating -= 10;
               }
               if (networkInterface.Name.Contains("VirtualBox")) {
                  rating -= 100;
               }
               if (ipProperties.MulticastAddresses.Any()) {
                  rating += 5;
               }
               if (ipv4Properties == null) {
                  rating -= 10;
               }
            }
            logger.Info($"Rating for interface {networkInterface.Name}: {rating}.");
            return rating;
         }

         private void BeginNextReceive() {
            var receiveState = receiveStatePool.TakeObject();
            receiveState.BeginReceive();
         }

         private void HandleReceiveCompleted(ReceiveState receiveState, byte[] buffer, int offset, int length, IPEndPoint remoteEndpoint) {
            DataArrived?.Invoke(network, buffer, offset, length, remoteEndpoint);
            receiveStatePool.ReturnObject(receiveState);
         }

         public void Broadcast(byte[] payload) {
            Broadcast(payload, 0, payload.Length);
         }
         
         public void Broadcast(byte[] payload, int offset, int length) {
            var e = broadcastSocketAsyncEventArgsPool.TakeObject();
            e.SetBuffer(payload, offset, length);
            e.RemoteEndPoint = multicastEndpoint;
            e.Completed += (sender, eventArgs) => {
               // doesn't seem like we can return this to the pool in a completed state.
               e.Dispose();
            };
            if(!socket.SendToAsync(e)) {
               broadcastSocketAsyncEventArgsPool.ReturnObject(e);
            }
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
               if(!socket.ReceiveFromAsync(socketEventArgs)) {
                  HandleReceiveCompleted(this, socketEventArgs);
               }
            }

            public void HandleReceiveCompleted(object sender, SocketAsyncEventArgs e) {
               networkContext.BeginNextReceive();
               networkContext.HandleReceiveCompleted(this, buffer, 0, e.BytesTransferred, (IPEndPoint)e.RemoteEndPoint);
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
