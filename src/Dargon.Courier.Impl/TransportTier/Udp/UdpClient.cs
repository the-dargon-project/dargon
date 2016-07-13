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
using Dargon.Commons.Comparers;
using Dargon.Commons.Exceptions;

namespace Dargon.Courier.TransportTier.Udp {
   public class UdpClientRemoteInfo {
      public IPEndPoint IPEndpoint { get; set; }
      public Socket Socket { get; set; }
   }

   public class UdpClient {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      private readonly IObjectPool<InboundDataEvent> inboundSomethingEventPool = ObjectPool.CreateStackBacked(() => new InboundDataEvent());
      private readonly IObjectPool<AsyncAutoResetLatch> asyncAutoResetEventPool = ObjectPool.CreateStackBacked(() => new AsyncAutoResetLatch());
      
      private readonly UdpTransportConfiguration configuration;
      private readonly List<Socket> multicastSockets;
      private readonly List<Socket> unicastSockets;
      private readonly IObjectPool<MemoryStream> outboundMemoryStreamPool = ObjectPool.CreateStackBacked(() => new MemoryStream(new byte[UdpConstants.kMaximumTransportSize], 0, UdpConstants.kMaximumTransportSize, true, true));
      private readonly IAuditAggregator<double> inboundBytesAggregator;
      private readonly IAuditAggregator<double> outboundBytesAggregator;
      private readonly IAuditAggregator<double> inboundReceiveProcessDispatchLatencyAggregator;

      private readonly IObjectPool<SocketAsyncEventArgs> sendArgsPool;

      private volatile bool isShutdown = false;
      private IUdpDispatcher udpDispatcher;

      private static int i = 0;

      private UdpClient(UdpTransportConfiguration configuration, List<Socket> multicastSockets, List<Socket> unicastSockets, IAuditAggregator<double> inboundBytesAggregator, IAuditAggregator<double> outboundBytesAggregator, IAuditAggregator<double> inboundReceiveProcessDispatchLatencyAggregator) {
         this.configuration = configuration;
         this.multicastSockets = multicastSockets;
         this.unicastSockets = unicastSockets;
         this.inboundBytesAggregator = inboundBytesAggregator;
         this.outboundBytesAggregator = outboundBytesAggregator;
         this.inboundReceiveProcessDispatchLatencyAggregator = inboundReceiveProcessDispatchLatencyAggregator;
         this.sendArgsPool = ObjectPool.CreateStackBacked(() => new SocketAsyncEventArgs());
      }

      public void StartReceiving(IUdpDispatcher udpDispatcher) {
         this.udpDispatcher = udpDispatcher;
         for (var i = 0; i < 64; i++) {
            multicastSockets.ForEach(s => BeginReceive(s, configuration.MulticastReceiveEndpoint));
         }
         for (var i = 0; i < 64; i++) {
            unicastSockets.ForEach(s => BeginReceive(s, configuration.UnicastReceiveEndpoint));
         }
         for (var i = 0; i < 64; i++) {
            multicastSockets.ForEach(s => {
               new Thread(() => BroadcastThreadStart(s)) { IsBackground = true }.Start();
            });
         }
      }

      private void BeginReceive(Socket socket, IPEndPoint remoteEndpoint) {
         var e = new SocketAsyncEventArgs {
            AcceptSocket = socket,
            RemoteEndPoint = new IPEndPoint(remoteEndpoint.Address, remoteEndpoint.Port)
         };
         e.SetBuffer(new byte[UdpConstants.kMaximumTransportSize], 0, UdpConstants.kMaximumTransportSize);
         e.UserToken = remoteEndpoint;
         e.Completed += HandleReceiveCompleted;

         try {
            socket.ReceiveFromAsync(e);
         } catch (ObjectDisposedException) when (isShutdown) {
            // socket was probably shut down
         }
      }

      private void HandleReceiveCompleted(object sender, SocketAsyncEventArgs e) {
         BeginReceive(e.AcceptSocket, (IPEndPoint)e.UserToken);

         var sw = new Stopwatch();
         sw.Start();

         var inboundSomethingEvent = inboundSomethingEventPool.TakeObject();
         inboundSomethingEvent.UdpClient = this;
         inboundSomethingEvent.SocketArgs = e;
         inboundSomethingEvent.Data = e.Buffer;
         inboundSomethingEvent.DataOffset = 0;
         inboundSomethingEvent.DataLength = e.BytesTransferred;
         inboundSomethingEvent.RemoteInfo = new UdpClientRemoteInfo {
            IPEndpoint = (IPEndPoint)e.RemoteEndPoint,
            Socket = e.AcceptSocket
         };
         inboundSomethingEvent.StopWatch = sw;

         udpDispatcher.HandleInboundDataEvent(inboundSomethingEvent, HandleInboundDataEventCompletionCallback);
      }

      private static void HandleInboundDataEventCompletionCallback(InboundDataEvent inboundSomethingEvent) {
         var self = inboundSomethingEvent.UdpClient;
         var e = inboundSomethingEvent.SocketArgs;
         var sw = inboundSomethingEvent.StopWatch;
         inboundSomethingEvent.Data = null;
         self.inboundSomethingEventPool.ReturnObject(inboundSomethingEvent);

         // analytics
         self.inboundBytesAggregator.Put(e.BytesTransferred);
         self.inboundReceiveProcessDispatchLatencyAggregator.Put(sw.ElapsedMilliseconds);

         // return to pool
         e.SetBuffer(null, 0, 0);
         e.Dispose();
      }

      private readonly ConcurrentQueue<Tuple<MemoryStream, int, int, Action>> todo = new ConcurrentQueue<Tuple<MemoryStream, int, int, Action>>();
      private readonly Semaphore sema = new Semaphore(0, int.MaxValue);

      public void BroadcastThreadStart(Socket socket) {
         while (true) {
            sema.WaitOne();
//            Console.WriteLine("!!!!!!! " + todo.Count);
            Tuple<MemoryStream, int, int, Action> x;
            if (!todo.TryDequeue(out x)) {
               throw new InvalidStateException();
            }
            var s = new ManualResetEvent(false);
            using (var e = new SocketAsyncEventArgs()) {
               e.RemoteEndPoint = configuration.MulticastSendEndpoint;
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
      }

      public void Unicast(UdpClientRemoteInfo remoteInfo, MemoryStream[] frames, Action action) {
         Array.Sort(frames, new MemoryStreamByReversePositionComparer());
         var outboundMemoryStreams = new List<MemoryStream>();
         foreach (var frame in frames) {
            var ms = outboundMemoryStreams.FirstOrDefault(x => x.Length - x.Position > frame.Position);
            if (ms == null) {
               ms = outboundMemoryStreamPool.TakeObject();
               outboundMemoryStreams.Add(ms);
            }
            ms.Write(frame.GetBuffer(), 0, (int)frame.Position);
         }
//         Console.WriteLine("Batched " + frames.Count + " to " + outboundMemoryStreams.Count);

         foreach (var outboundMemoryStream in outboundMemoryStreams) {
            var s = new ManualResetEvent(false);
            using (var e = new SocketAsyncEventArgs()) {
               e.RemoteEndPoint = remoteInfo.IPEndpoint;
               e.SetBuffer(outboundMemoryStream.GetBuffer(), 0, (int)outboundMemoryStream.Position);
               e.Completed += (sender, args) => {
                  s.Set();
               };
               try {
                  if (!remoteInfo.Socket.SendToAsync(e)) {
                     // Completed synchronously. e.Completed won't be called.
                     // pooling was leading to leaks?
                  } else {
                     s.WaitOne();
                  }
               } catch (ObjectDisposedException) when (isShutdown) { }
               e.SetBuffer(null, 0, 0);
               outboundBytesAggregator.Put(outboundMemoryStream.Length);
            }
            outboundMemoryStream.SetLength(0);
            outboundMemoryStreamPool.ReturnObject(outboundMemoryStream);
         }

         action();
      }

      public void Shutdown() {
         isShutdown = true;
         foreach (var socket in multicastSockets) {
            socket.Close();
            socket.Dispose();
         }
         foreach (var socket in unicastSockets) {
            socket.Close();
            socket.Dispose();
         }
      }

      public class MemoryStreamByReversePositionComparer : IComparer<MemoryStream> {
         public int Compare(MemoryStream x, MemoryStream y) => -x.Position.CompareTo(y.Position);
      }

      public static UdpClient Create(UdpTransportConfiguration udpTransportConfiguration, IAuditAggregator<double> inboundBytesAggregator, IAuditAggregator<double> outboundBytesAggregator, IAuditAggregator<double> inboundReceiveProcessDispatchLatencyAggregator) {
         var multicastSockets = new List<Socket>();
         var unicastSockets = new List<Socket>();
         foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces()) {
            if (!networkInterface.SupportsMulticast ||
                networkInterface.OperationalStatus != OperationalStatus.Up ||
                networkInterface.IsReceiveOnly) continue;

//             HACK loopback disable
            if (networkInterface.Name.Contains("3")) continue;

            var ipv4Properties = networkInterface.GetIPProperties()?.GetIPv4Properties();
            if (ipv4Properties != null) {
               multicastSockets.Add(CreateMulticastSocket(ipv4Properties.Index, udpTransportConfiguration));
               if (!udpTransportConfiguration.MulticastReceiveEndpoint.Equals(udpTransportConfiguration.UnicastReceiveEndpoint)) {
                  unicastSockets.Add(CreateUnicastSocket(ipv4Properties.Index, udpTransportConfiguration));
               }
            }

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
         return new UdpClient(udpTransportConfiguration, multicastSockets, unicastSockets, inboundBytesAggregator, outboundBytesAggregator, inboundReceiveProcessDispatchLatencyAggregator);
      }

      private static Socket CreateMulticastSocket(long adapterIndex, UdpTransportConfiguration udpTransportConfiguration) {
         var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) {
            DontFragment = false,
            MulticastLoopback = true
         };
         socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
         socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(udpTransportConfiguration.MulticastAddress));
         socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 0); //0: localhost, 1: lan (via switch)
         socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, (int)IPAddress.HostToNetworkOrder(adapterIndex));
         socket.Bind(new IPEndPoint(IPAddress.Any, udpTransportConfiguration.MulticastReceiveEndpoint.Port));
         return socket;
      }

      private static Socket CreateUnicastSocket(long adapterIndex, UdpTransportConfiguration udpTransportConfiguration) {
         var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) {
            DontFragment = false
         };
         socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
         socket.Bind(new IPEndPoint(IPAddress.Any, udpTransportConfiguration.UnicastReceiveEndpoint.Port));
         return socket;
      }
   }
}
