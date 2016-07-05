using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Pooling;
using Dargon.Courier.AuditingTier;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.Collections;
using Dargon.Commons.Exceptions;

namespace Dargon.Courier.TransportTier.Udp {
   public class UdpClient {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      private readonly IObjectPool<InboundDataEvent> inboundSomethingEventPool = ObjectPool.CreateStackBacked(() => new InboundDataEvent());
      private readonly IObjectPool<AsyncAutoResetLatch> asyncAutoResetEventPool = ObjectPool.CreateStackBacked(() => new AsyncAutoResetLatch());
      
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
         for (var i = 0; i < 64; i++) {
            sockets.ForEach(BeginReceive);
         }
         for (var i = 0; i < 32; i++) {
            sockets.ForEach(s => {
               new Thread(() => TBroadcastStart(s)) { IsBackground = true }.Start();
            });
         }
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
//         BeginReceive(e.AcceptSocket);
         HandleReceiveCompletedHelper(e);
      }

      private void HandleReceiveCompletedHelper(SocketAsyncEventArgs e) {
//         logger.Debug($"Received from {e.RemoteEndPoint} {e.BytesTransferred} bytes!");
         var sw = new Stopwatch();
         sw.Start();

         var inboundSomethingEvent = inboundSomethingEventPool.TakeObject();
         inboundSomethingEvent.Data = e.Buffer;

         udpDispatcher.HandleInboundDataEvent(
            inboundSomethingEvent,
            () => {
               inboundSomethingEvent.Data = null;
               inboundSomethingEventPool.ReturnObject(inboundSomethingEvent);

               // analytics
               inboundBytesAggregator.Put(e.BytesTransferred);
               inboundReceiveProcessDispatchLatencyAggregator.Put(sw.ElapsedMilliseconds);

               // return to pool
               try {
                  e.AcceptSocket.ReceiveFromAsync(e);
               } catch (ObjectDisposedException) when (isShutdown) {
                  // socket was probably shut down
               }
               //               receiveArgsPool.ReturnObject(e);
            });
      }

      private readonly ConcurrentQueue<Tuple<MemoryStream, int, int, Action>> todo = new ConcurrentQueue<Tuple<MemoryStream, int, int, Action>>();
      private readonly Semaphore sema = new Semaphore(0, int.MaxValue);

      public void TBroadcastStart(Socket socket) {
         while (true) {
            sema.WaitOne();
            Tuple<MemoryStream, int, int, Action> x;
            if (!todo.TryDequeue(out x)) {
               throw new InvalidStateException();
            }
            var s = new ManualResetEvent(false);
            using (var e = new SocketAsyncEventArgs()) {
               e.RemoteEndPoint = configuration.SendEndpoint;
               e.SetBuffer(x.Item1.GetBuffer(), x.Item2, x.Item3);
               e.Completed += (sender, args) => {
                  s.Set();
               };
               try {
                  if (!socket.SendToAsync(e)) {
                     // Completed synchronously. e.Completed won't be called.
                     // pooling was leading to leaks?
                  } else {
                     s.WaitOne();
                  }
               } catch (ObjectDisposedException) when (isShutdown) { }
               e.SetBuffer(null, 0, 0);
               x.Item4();
               outboundBytesAggregator.Put(x.Item3);
            }
         }
      }

      public void Broadcast(MemoryStream ms, int offset, int length, Action done) {
         todo.Enqueue(Tuple.Create(ms, offset, length, done));
         sema.Release();
         return;
         var e = new SocketAsyncEventArgs();
         e.RemoteEndPoint = configuration.SendEndpoint;
         e.SetBuffer(ms.GetBuffer(), offset, length);
         e.Completed += (sender, args) => {
            e.SetBuffer(null, 0, 0);
            args.Dispose();
            e.Dispose();
            outboundBytesAggregator.Put(length);
            done();
         };
         try {
            var socket = sockets.First();
            if (!socket.SendToAsync(e)) {
               // Completed synchronously. e.Completed won't be called.
               // pooling was leading to leaks?
               e.SetBuffer(null, 0, 0);
               e.Dispose();
               done();
               outboundBytesAggregator.Put(length);
            }
         } catch (ObjectDisposedException) when (isShutdown) { }
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

//             HACK loopback disable
            if (networkInterface.Name.Contains("3")) continue;

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
         socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 0); //0: localhost, 1: lan (via switch)
         socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, (int)IPAddress.HostToNetworkOrder(adapterIndex));
         socket.Bind(new IPEndPoint(IPAddress.Any, udpTransportConfiguration.ReceiveEndpoint.Port));
         return socket;
      }
   }
}
