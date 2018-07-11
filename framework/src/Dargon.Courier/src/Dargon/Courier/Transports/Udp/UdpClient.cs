using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Collections;
using Dargon.Commons.Comparers;
using Dargon.Commons.Exceptions;
using Dargon.Commons.Pooling;
using Dargon.Courier.AuditingTier;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.Scheduler;

namespace Dargon.Courier.TransportTier.Udp {
   public class UdpClientRemoteInfo {
      public IPEndPoint IPEndpoint { get; set; }
      public Socket Socket { get; set; }
   }

   public class UdpClient {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      // todo figure out better pooling
      private readonly IObjectPool<InboundDataEvent> inboundSomethingEventPool = ObjectPool.CreateConcurrentQueueBacked(() => new InboundDataEvent());
      
      private readonly UdpTransportConfiguration configuration;
      private readonly List<Socket> multicastSockets;
      private readonly List<Socket> unicastSockets;
      private readonly IJobQueue<UdpUnicastJob> unicastJobQueue;
      private readonly IObjectPool<byte[]> sendReceiveBufferPool;

      // todo figure out better pooling
      private readonly IObjectPool<MemoryStream> outboundMemoryStreamPool = ObjectPool.CreateConcurrentQueueBacked(() => new MemoryStream(new byte[UdpConstants.kMaximumTransportSize], 0, UdpConstants.kMaximumTransportSize, true, true));
      private readonly IAuditAggregator<double> inboundBytesAggregator;
      private readonly IAuditAggregator<double> outboundBytesAggregator;
      private readonly IAuditAggregator<double> inboundReceiveProcessDispatchLatencyAggregator;

      private volatile bool isShutdown = false;
      private IUdpDispatcher udpDispatcher;

      private static int i = 0;

      private UdpClient(UdpTransportConfiguration configuration, List<Socket> multicastSockets, List<Socket> unicastSockets, IJobQueue<UdpUnicastJob> unicastJobQueue, IObjectPool<byte[]> sendReceiveBufferPool, IAuditAggregator<double> inboundBytesAggregator, IAuditAggregator<double> outboundBytesAggregator, IAuditAggregator<double> inboundReceiveProcessDispatchLatencyAggregator) {
         this.configuration = configuration;
         this.multicastSockets = multicastSockets;
         this.unicastSockets = unicastSockets;
         this.unicastJobQueue = unicastJobQueue;
         this.sendReceiveBufferPool = sendReceiveBufferPool;
         this.inboundBytesAggregator = inboundBytesAggregator;
         this.outboundBytesAggregator = outboundBytesAggregator;
         this.inboundReceiveProcessDispatchLatencyAggregator = inboundReceiveProcessDispatchLatencyAggregator;
      }

      public void StartReceiving(IUdpDispatcher udpDispatcher) {
         this.udpDispatcher = udpDispatcher;
         for (var i = 0; i < 8; i++) {
            multicastSockets.ForEach(s => BeginReceive(s, configuration.MulticastReceiveEndpoint));
         }
         for (var i = 0; i < 8; i++) {
            unicastSockets.ForEach(s => BeginReceive(s, configuration.UnicastReceiveEndpoint));
         }
         for (var i = 0; i < 4; i++) {
            multicastSockets.ForEach(s => {
               new Thread(() => BroadcastThreadStart(s)) { IsBackground = true, Name = $"Udp_Broadcast_{i}" }.Start();
            });
         }
      }

      private void BeginReceive(Socket socket, IPEndPoint remoteEndpoint) {
         var e = new SocketAsyncEventArgs {
            AcceptSocket = socket,
            RemoteEndPoint = new IPEndPoint(remoteEndpoint.Address, remoteEndpoint.Port)
         };
         e.SetBuffer(sendReceiveBufferPool.TakeObject(), 0, UdpConstants.kMaximumTransportSize);
         e.UserToken = remoteEndpoint;
         e.Completed += HandleReceiveCompleted;

         try {
            bool pending = socket.ReceiveFromAsync(e);
            if (!pending) {
               HandleReceiveCompleted(socket, e);
            }
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
         inboundSomethingEvent.DataBufferPool = sendReceiveBufferPool;
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

         inboundSomethingEvent.DataBufferPool.ReturnObject(inboundSomethingEvent.Data);
         inboundSomethingEvent.Data = null;
         self.inboundSomethingEventPool.ReturnObject(inboundSomethingEvent);

         // analytics
         self.inboundBytesAggregator.Put(e.BytesTransferred);
         self.inboundReceiveProcessDispatchLatencyAggregator.Put(sw.ElapsedMilliseconds);

         // return to pool
         try {
            e.Dispose();

//            var referenceRemoteEndpoint = (IPEndPoint)e.UserToken;
//            e.RemoteEndPoint = new IPEndPoint(referenceRemoteEndpoint.Address, referenceRemoteEndpoint.Port);
//            e.AcceptSocket.ReceiveFromAsync(e);
         } catch (ObjectDisposedException) when (self.isShutdown) {
            // socket was probably shut down
         }
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
         // Frames larger than half the maximum packet size certainly cannot be packed together.
         var smallFrames = new List<MemoryStream>(frames.Length);
         var largeFrames = new List<MemoryStream>(frames.Length);
         const int kHalfMaximumTransportSize = UdpConstants.kMaximumTransportSize / 2;
         for (var i = 0; i < frames.Length; i++) {
            var frame = frames[i];
            if (frame.Length <= kHalfMaximumTransportSize) {
               smallFrames.Add(frame);
            } else {
               largeFrames.Add(frame);
            }
         }

         // Order small frames ascending by size, large frames descending by size.
         smallFrames.Sort(new MemoryStreamByPositionComparer());
         largeFrames.Sort(new ReverseComparer<MemoryStream>(new MemoryStreamByPositionComparer()));

         // Place large frames into outbound buffers.
         var outboundBuffers = new List<MemoryStream>(frames.Length);
         foreach (var largeFrame in largeFrames) {
            var outboundBuffer = outboundMemoryStreamPool.TakeObject();
            outboundBuffer.Write(largeFrame.GetBuffer(), 0, (int)largeFrame.Position);
            outboundBuffers.Add(outboundBuffer);
         }

         // Place small frames into outbound buffers. Note that as the
         // small frames are ascending in size and the buffers are descending
         // in size, while we iterate if a small frame cannot fit into the
         // next outbound buffer then none of the following small frames can either.
         int activeOutboundBufferIndex = 0;
         foreach (var smallFrame in smallFrames) {
            // precompute greatest outbound buffer permission for which
            // we will still be able to fit into the buffer.
            int frameSize = (int)smallFrame.Position;
            int greatestFittableBufferPosition = UdpConstants.kMaximumTransportSize - frameSize;

            // Attempt to place the small frame into existing outbound buffers
            bool placed = false;
            while (!placed && activeOutboundBufferIndex != outboundBuffers.Count) {
               var outboundBuffer = outboundBuffers[activeOutboundBufferIndex];
               if (outboundBuffer.Position > greatestFittableBufferPosition) {
                  activeOutboundBufferIndex++;
               } else {
                  outboundBuffer.Write(smallFrame.GetBuffer(), 0, (int)smallFrame.Position);
                  placed = true;
               }
            }

            // If no existing outbound buffer had space, allocate a new one
            if (!placed) {
               Assert.Equals(outboundBuffers.Count, activeOutboundBufferIndex);
               var outboundBuffer = outboundMemoryStreamPool.TakeObject();
               outboundBuffer.Write(smallFrame.GetBuffer(), 0, (int)smallFrame.Position);
               outboundBuffers.Add(outboundBuffer);
            }
         }

//         Console.WriteLine($"Batched {frames.Length} to {outboundBuffers.Count} buffers.");

         int sendsRemaining = outboundBuffers.Count;
         foreach (var outboundBuffer in outboundBuffers) {
            var job = new UdpUnicastJob {
               OutboundBuffer = outboundBuffer,
               RemoteInfo = remoteInfo,
               SendCompletionHandler = () => {
                  outboundBuffer.SetLength(0);
                  outboundMemoryStreamPool.ReturnObject(outboundBuffer);
                  if (Interlocked.Decrement(ref sendsRemaining) == 0) {
                     action();
                  }
               }
            };
            unicastJobQueue.Enqueue(job);
         }

//         int sendsRemaining = outboundBuffers.Count;
//         Parallel.ForEach(
//            outboundBuffers,
//            outboundBuffer => {
//               outboundBytesAggregator.Put(outboundBuffer.Length);
//
//               var e = new SocketAsyncEventArgs();
//               e.RemoteEndPoint = remoteInfo.IPEndpoint;
//               e.SetBuffer(outboundBuffer.GetBuffer(), 0, (int)outboundBuffer.Position);
//               e.Completed += (sender, args) => {
//                  // Duplicate code with below.
//                  args.SetBuffer(null, 0, 0);
//                  args.Dispose();
//
//                  outboundBuffer.SetLength(0);
//                  outboundMemoryStreamPool.ReturnObject(outboundBuffer);
//
//                  if (Interlocked.Decrement(ref sendsRemaining) == 0) {
//                     action();
//                  }
//               };
//
//               const int kSendStateAsync = 1;
//               const int kSendStateDone = 2;
//               const int kSendStateError = 3;
//               int sendState;
//               try {
//                  bool completingAsynchronously = remoteInfo.Socket.SendToAsync(e);
//                  sendState = completingAsynchronously ? kSendStateAsync : kSendStateDone;
//               } catch (ObjectDisposedException) when (isShutdown) {
//                  sendState = kSendStateError;
//               }
//               
//               if (sendState == kSendStateDone || sendState == kSendStateError) {
//                  // Completed synchronously so e.Completed won't be called.
//                  e.SetBuffer(null, 0, 0);
//                  e.Dispose();
//               
//                  outboundBuffer.SetLength(0);
//                  outboundMemoryStreamPool.ReturnObject(outboundBuffer);
//
//                  if (Interlocked.Decrement(ref sendsRemaining) == 0) {
//                     action();
//                  }
//               }
//            });

//         int sendsRemaining = outboundBuffers.Count;
//         foreach (var outboundBuffer in outboundBuffers) {
//            outboundBytesAggregator.Put(outboundBuffer.Length);
//            
//            var e = new SocketAsyncEventArgs();
//            e.RemoteEndPoint = remoteInfo.IPEndpoint;
//            e.SetBuffer(outboundBuffer.GetBuffer(), 0, (int)outboundBuffer.Position);
//            e.Completed += (sender, args) => {
//               // Duplicate code with below.
//               args.SetBuffer(null, 0, 0);
//               args.Dispose();
//
//               outboundBuffer.SetLength(0);
//               outboundMemoryStreamPool.ReturnObject(outboundBuffer);
//
//               if (Interlocked.Decrement(ref sendsRemaining) == 0) {
//                  action();
//               }
//            };
//
//            const int kSendStateAsync = 1;
//            const int kSendStateDone = 2;
//            const int kSendStateError = 3;
//            int sendState;
//            try {
//               bool completingAsynchronously = remoteInfo.Socket.SendToAsync(e);
//               sendState = completingAsynchronously ? kSendStateAsync : kSendStateDone;
//            } catch (ObjectDisposedException) when (isShutdown) {
//               sendState = kSendStateError;
//            }
//
//            if (sendState == kSendStateDone || sendState == kSendStateError) {
//               // Completed synchronously so e.Completed won't be called.
//               e.SetBuffer(null, 0, 0);
//               e.Dispose();
//
//               outboundBuffer.SetLength(0);
//               outboundMemoryStreamPool.ReturnObject(outboundBuffer);
//
//               if (sendState == kSendStateError) {
//                  // Don't send remaining messages.
//                  // To the application, this appears like packet loss.
//                  action();
//                  return;
//               } else if (Interlocked.Decrement(ref sendsRemaining) == 0) {
//                  action();
//               }
//            }
//         }
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

      public class MemoryStreamByPositionComparer : IComparer<MemoryStream> {
         public int Compare(MemoryStream x, MemoryStream y) => x.Position.CompareTo(y.Position);
      }

      public class UdpUnicastJob {
         public UdpClientRemoteInfo RemoteInfo { get; set; }
         public MemoryStream OutboundBuffer { get; set; }
         public Action SendCompletionHandler { get; set; }
      }

      public static UdpClient Create(UdpTransportConfiguration udpTransportConfiguration, IScheduler udpUnicastScheduler, IObjectPool<byte[]> sendReceiveBufferPool, IAuditAggregator<double> inboundBytesAggregator, IAuditAggregator<double> outboundBytesAggregator, IAuditAggregator<double> inboundReceiveProcessDispatchLatencyAggregator) {
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

         var unicastJobQueue = udpUnicastScheduler.CreateJobQueue<UdpUnicastJob>(
            job => {
               var outboundBuffer = job.OutboundBuffer;
               outboundBytesAggregator.Put(outboundBuffer.Length);

               var e = new SocketAsyncEventArgs();
               e.RemoteEndPoint = job.RemoteInfo.IPEndpoint;
               e.SetBuffer(outboundBuffer.GetBuffer(), 0, (int)outboundBuffer.Position);
               e.Completed += (sender, args) => {
                  // Duplicate code with below.
                  args.SetBuffer(null, 0, 0);
                  args.Dispose();

                  job.SendCompletionHandler();
               };

               const int kSendStateAsync = 1;
               const int kSendStateDone = 2;
               const int kSendStateError = 3;
               int sendState;
               try {
                  bool completingAsynchronously = job.RemoteInfo.Socket.SendToAsync(e);
                  sendState = completingAsynchronously ? kSendStateAsync : kSendStateDone;
               } catch (ObjectDisposedException) {
                  sendState = kSendStateError;
               }

               if (sendState == kSendStateDone || sendState == kSendStateError) {
                  // Completed synchronously so e.Completed won't be called.
                  e.SetBuffer(null, 0, 0);
                  e.Dispose();

                  job.SendCompletionHandler();
               }
            });

         return new UdpClient(udpTransportConfiguration, multicastSockets, unicastSockets, unicastJobQueue, sendReceiveBufferPool, inboundBytesAggregator, outboundBytesAggregator, inboundReceiveProcessDispatchLatencyAggregator);
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
