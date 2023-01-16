using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.AsyncAwait;
using Dargon.Commons.Collections;
using Dargon.Commons.Pooling;
using Dargon.Commons.Utilities;
using static Dargon.Courier.TransportTier.Udp.CoreUdp;

namespace Dargon.Courier.TransportTier.Udp {
   public struct UdpRemoteInfo {
      public required IPEndPoint IPEndpoint { get; init; }
      public required IOpaqueUdpNetworkAdapter NetworkContext { get; init; }
   }

   public interface IOpaqueUdpNetworkAdapter { }

   public interface ICoreUdpReceiveListener {
      /// <param name="leasedBufferView">reference count must be decremented by the callee</param>
      void HandleInboundUdpPacket(IOpaqueUdpNetworkAdapter adapter, LeasedBufferView leasedBufferView, UdpRemoteInfo remoteInfo);
   }

   public class LeasedBufferViewPool {
      private readonly IObjectPool<LeasedBufferView> innerPool;

      public LeasedBufferViewPool(int bufferCapacity) : this(new ConcurrentQueueBackedObjectPool<LeasedBufferView>(pool => new LeasedBufferView(pool, bufferCapacity))) { }

      public LeasedBufferViewPool(IObjectPool<LeasedBufferView> innerPool) {
         this.innerPool = innerPool;
      }

      /// <returns>Reference count must be decremented once</returns>
      public LeasedBufferView Acquire() {
         var lbv = innerPool.TakeObject();
         lbv.Init();
         return lbv;
      }
   }

   public class CoreUdp {
      private static readonly LeasedBufferViewPool bufferLeasePool = new(UdpConstants.kMaximumTransportSize);
      public static LeasedBufferView AcquireLeasedBufferView() => bufferLeasePool.Acquire();

      private readonly UdpTransportConfiguration udpTransportConfiguration;
      private readonly CourierSynchronizationContexts synchronizationContexts;
      
      private readonly ReaderWriterLockSlim rwls = new();
      private readonly List<RawUdpSocketContext> allUnics = new();

      private readonly ConcurrentSet<ICoreUdpReceiveListener> receiveListeners = new();

      public CoreUdp(UdpTransportConfiguration udpTransportConfiguration, CourierSynchronizationContexts synchronizationContexts) {
         this.udpTransportConfiguration = udpTransportConfiguration;
         this.synchronizationContexts = synchronizationContexts;

         DetectInitialNetworkInterfaces();
      }

      private void DetectInitialNetworkInterfaces() {
         foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces()) {
            if (!networkInterface.SupportsMulticast ||
                networkInterface.OperationalStatus != OperationalStatus.Up ||
                networkInterface.IsReceiveOnly) continue;

            var ipv4Properties = networkInterface.GetIPProperties()?.GetIPv4Properties();
            if (ipv4Properties != null) {
               OnIpv4NetworkInterfaceUp(networkInterface, ipv4Properties);
            }
         }
      }

      private void OnIpv4NetworkInterfaceUp(NetworkInterface networkInterface, IPv4InterfaceProperties ipv4Properties) {
         using var _ = rwls.CreateWriterGuard();

         var addrs = networkInterface.GetIPProperties().UnicastAddresses.ToArray()
                                     .FilterTo(x => x.Address.AddressFamily == AddressFamily.InterNetwork)
                                     .Join(", ");
         Console.WriteLine($"NIC Up: {networkInterface.Name}, Addrs: {addrs}");

         foreach (var isMulticast in new BoolEnumerator()) {
            allUnics.Add(new RawUdpSocketContext {
               CoreUdp = this,
               NetworkInterface = networkInterface,
               Ipv4InterfaceProperties = ipv4Properties,
               UdpTransportConfiguration = udpTransportConfiguration,
               IsMulticastSession = isMulticast,
               SynchronizationContexts = synchronizationContexts,
            }.Tap(static x => x.Initialize()));
         }
      }

      public void AddReceiveListener(ICoreUdpReceiveListener receiveListener) {
         this.receiveListeners.AddOrThrow(receiveListener);
      }

      /// <param name="leasedBufferView">Reference count must be decremented by the callee</param>
      private void HandleReceiveAsync(RawUdpSocketContext context, LeasedBufferView leasedBufferView, UdpRemoteInfo remoteInfo) {
         foreach (var rl in receiveListeners) {
            rl.HandleInboundUdpPacket(context, leasedBufferView.Share, remoteInfo);
         }

         leasedBufferView.Release();
      }

      public class RawUdpSocketContext : IOpaqueUdpNetworkAdapter {
         public required CoreUdp CoreUdp { get; init; }
         public required NetworkInterface NetworkInterface { get; init; }
         public required IPv4InterfaceProperties Ipv4InterfaceProperties { get; init; }
         public required UdpTransportConfiguration UdpTransportConfiguration { get; init; }
         public required bool IsMulticastSession { get; init; }
         public required CourierSynchronizationContexts SynchronizationContexts { get; init; }

         private Socket socket;
         private IPEndPoint receiveEndpoint;

         public void Initialize() {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) {
               DontFragment = false,
            };
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

            if (IsMulticastSession) {
               socket.MulticastLoopback = true;
               socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(UdpTransportConfiguration.MulticastAddress));
               socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1); // 0: localhost, 1: lan (via switch)
               socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, (int)IPAddress.HostToNetworkOrder(Ipv4InterfaceProperties.Index));
            }

            receiveEndpoint = IsMulticastSession ? UdpTransportConfiguration.MulticastReceiveEndpoint : UdpTransportConfiguration.UnicastReceiveEndpoint;
            socket.Bind(new IPEndPoint(IPAddress.Any, receiveEndpoint.Port));

            ReceiveLoopAsync().Forget();
         }

         private async Task ReceiveLoopAsync() {
            await SynchronizationContexts.EarlyNetworkIO.YieldToAsync();

            var bufferLease = AcquireLeasedBufferView();

            var res = await socket.ReceiveFromAsync(bufferLease.RawBuffer, receiveEndpoint);
            if (res.ReceivedBytes <= 0) return; // indicates socket close.

            Task.Run(ReceiveLoopAsync).Forget();

            var remoteInfo = new UdpRemoteInfo {
               IPEndpoint = receiveEndpoint,
               NetworkContext = this,
            };
            CoreUdp.HandleReceiveAsync(this, bufferLease.Transfer, remoteInfo);
         }

         public void S() {
            // var e =  new SocketAsyncEventArgs();
            // e.BufferList
            var zz = new int[2][];
            socket.Send(new ArraySegment<byte>[1]);
         }
      }

      public void Unicast(UdpRemoteInfo remoteInfo, MemoryStream[] map, Action action) {
         throw new NotImplementedException();
      }

      public void Shutdown() { }
   }
}
