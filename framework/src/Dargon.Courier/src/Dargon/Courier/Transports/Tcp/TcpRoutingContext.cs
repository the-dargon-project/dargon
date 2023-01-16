using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Exceptions;
using Dargon.Courier.AccessControlTier;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;
using Dargon.Courier.SessionTier;
using Dargon.Courier.TransportTier.Tcp.Vox;
using Dargon.Courier.Vox;
using NLog;
using static Dargon.Commons.Channels.ChannelsExtensions;

namespace Dargon.Courier.TransportTier.Tcp.Server {
   public class TcpRoutingContext : IRoutingContext {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      private readonly ConcurrentQueue<MessageDto> outboundMessageQueue = new ConcurrentQueue<MessageDto>();
      private readonly ConcurrentDictionary<MessageDto, AsyncLatch> sendCompletionLatchByMessage = new ConcurrentDictionary<MessageDto, AsyncLatch>();
      private readonly AsyncSemaphore outboundMessageSignal = new AsyncSemaphore(0);
      private readonly AsyncLock writerLock = new AsyncLock();
      private readonly CancellationTokenSource shutdownCancellationTokenSource = new CancellationTokenSource();

      private readonly TcpTransportConfiguration configuration;
      private readonly TcpRoutingContextContainer tcpRoutingContextContainer;
      private readonly Socket client;
      private readonly InboundMessageDispatcher inboundMessageDispatcher;
      private readonly Identity localIdentity;
      private readonly NetworkStream ns;
      private readonly PeerTable peerTable;
      private readonly PayloadUtils payloadUtils;
      private readonly IGatekeeper gatekeeper;
      private readonly RoutingTable routingTable;

      private readonly AsyncReaderWriterLock stateLock = new();
      private Identity remoteIdentity;
      private PeerContext peerContext;
      private Task runAsyncInnerTask;
      private volatile bool isShutdown = false;

      public TcpRoutingContext(TcpTransportConfiguration configuration, TcpRoutingContextContainer tcpRoutingContextContainer, Socket client, InboundMessageDispatcher inboundMessageDispatcher, Identity localIdentity, RoutingTable routingTable, PeerTable peerTable, PayloadUtils payloadUtils, IGatekeeper gatekeeper) {
         logger.Debug($"Constructing TcpRoutingContext for client {client.RemoteEndPoint}, localId: {localIdentity}");

         this.configuration = configuration;
         this.tcpRoutingContextContainer = tcpRoutingContextContainer;
         this.client = client;
         this.inboundMessageDispatcher = inboundMessageDispatcher;
         this.localIdentity = localIdentity;
         this.routingTable = routingTable;
         this.peerTable = peerTable;
         this.payloadUtils = payloadUtils;
         this.gatekeeper = gatekeeper;
         this.ns = new NetworkStream(client);
      }

      public async Task RunAsync() {
         Log($"Entered RunAsync.");
         runAsyncInnerTask = RunAsyncHelper();
         await runAsyncInnerTask;
         Log($"Left RunAsync.");
      }

      private async Task RunAsyncHelper() {
         try {
            var shutdownToken = shutdownCancellationTokenSource.Token;
            var readRemoteHandshakeTask = payloadUtils.ReadPayloadAsync(ns, shutdownToken);
            var sendLocalHandshakeTask = payloadUtils.WritePayloadAsync(ns, new HandshakeDto {
               Identity = localIdentity, 
               AdditionalParameters = configuration.AdditionalHandshakeParameters
            }, writerLock, shutdownToken);
            await Task.WhenAny( // WhenAny yields the first complete task. Await that task via Unwrap to validate timeout didn't happen.
               new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token.AsTask(),
               Task.WhenAll(readRemoteHandshakeTask, sendLocalHandshakeTask)).Unwrap();

            var remoteHandshake = (HandshakeDto)await readRemoteHandshakeTask;
            remoteIdentity = remoteHandshake.Identity.AssertIsNotNull();

            // Prior to the gatekeeper line, the remote identity can be forged.
            // Past the gatekeeper line, the remote identity is validated.
            gatekeeper.ValidateClientHandshake(remoteHandshake);

            tcpRoutingContextContainer.AssociateRemoteIdentityOrThrow(remoteIdentity.Id, this);
            routingTable.Register(remoteIdentity.Id, this);

            peerContext = peerTable.GetOrAdd(remoteIdentity.Id);
            CourierAmbientPeerContext.CurrentContext.AssertIsNull();
            var session = peerContext.GetSession().UseAsImplicitAsyncLocalContext();
            gatekeeper.LoadSessionState(remoteHandshake, session);

            peerContext.HandleInboundPeerIdentityUpdate(remoteIdentity);

            configuration.HandleRemoteHandshakeCompletion(remoteIdentity);

            try {
               CourierAmbientPeerContext.CurrentContext.AssertEquals(session);

               var readerTask = RunReaderAsync(shutdownToken).Forgettable();
               var writerTask = RunWriterAsync(shutdownToken).Forgettable();

               await Task.WhenAny(
                  readerTask,
                  writerTask,
                  shutdownToken.AsTask()
               ).ConfigureAwait(false);
               shutdownCancellationTokenSource.Cancel();
            } catch (OperationCanceledException) when (isShutdown) { }
         } catch (Exception e) {
            Log($"RunAsync threw {e}", LogLevel.Error);
         } finally {
            if (remoteIdentity != null) {
               routingTable.Unregister(remoteIdentity.Id, this);
               tcpRoutingContextContainer.TryUnassociateRemoteIdentity(remoteIdentity.Id, this);
            }
            tcpRoutingContextContainer.RemoveOrThrow(this);
         }
      }

      private async Task RunReaderAsync(CancellationToken token) {
         Log($"Entered RunReaderAsync.");
         try {
            while (!isShutdown) {
               var payload = await payloadUtils.ReadPayloadAsync(ns, token);
               if (payload is MessageDto) {
                  inboundMessageDispatcher.DispatchAsync((MessageDto)payload).Forget();
               }
            }
         } catch (ObjectDisposedException) when (isShutdown) {
            // shutdown
         } catch (TaskCanceledException) when (isShutdown) {
            // shutdown
         } catch (IOException) when (isShutdown) {
            // shutdown
         } catch (Exception e) {
            Log($"RunReaderAsync threw {e}", LogLevel.Error);
         }

         Log($"Exiting RunReaderAsync.");
         ShutdownAsync().Forget();
      }

      private async Task RunWriterAsync(CancellationToken token) {
         Log($"Entered runWriterAsync.");
         try {
            while (!isShutdown) {
               await outboundMessageSignal.WaitAsync(token);
               Go(async () => {
                  Log($"Entered message writer task.");
                  MessageDto message;
                  if (!outboundMessageQueue.TryDequeue(out message)) {
                     throw new InvalidStateException();
                  }

                  Log($"Writing message {message} destination {message.ReceiverId.ToString("n").Substring(0, 6)}.");
                  await payloadUtils.WritePayloadAsync(ns, message, writerLock, token);
                  Log($"Wrote message {message} destination {message.ReceiverId.ToString("n").Substring(0, 6)}.");
                  sendCompletionLatchByMessage[message].SetOrThrow();;
                  Log($"Exiting message writer task.");
               }).Forget();
            }
         } catch (ObjectDisposedException) when (isShutdown) {
            // shutdown
         } catch (TaskCanceledException) when (isShutdown) {
            // shutdown
         } catch (IOException) when (isShutdown) {
            // shutdown
         } catch (Exception e) {
            Log($"runWriterAsync threw {e}", LogLevel.Error);
         }

         Log($"exiting runWriterAsync", LogLevel.Debug);
         ShutdownAsync().Forget();
      }

      public int Weight { get; }

      public Task SendBroadcastAsync(MessageDto message) {
         return SendHelperAsync(Guid.Empty, message);
      }

      public Task SendUnreliableAsync(Guid destination, MessageDto message) {
         return SendHelperAsync(destination, message);
      }

      public Task SendReliableAsync(Guid destination, MessageDto message) {
         return SendHelperAsync(destination, message);
      }

      private Task SendHelperAsync(Guid destination, MessageDto message) {
         Log(
            $"Sending to {destination.ToString("n").Substring(0, 6)} message {message}. " + Environment.NewLine +
            $"clientIdentity matches destination: {remoteIdentity.Matches(destination, IdentityMatchingScope.Broadcast)}");
         if (remoteIdentity == null || !remoteIdentity.Matches(destination, IdentityMatchingScope.Broadcast)) {
            return Task.CompletedTask;
         }

         var completionLatch = new AsyncLatch();
         sendCompletionLatchByMessage.AddOrThrow(message, completionLatch);

         outboundMessageQueue.Enqueue(message);
         outboundMessageSignal.Release();

         return SendHelperWaitForCompletionLatchAndCleanupAsync(destination, message, completionLatch);

      }

      private async Task SendHelperWaitForCompletionLatchAndCleanupAsync(Guid destination, MessageDto message, AsyncLatch completionLatch) {
         Log($"Awaiting completion for send to {destination.ToString("n").Substring(0, 6)} message {message}.");
         await completionLatch.WaitAsync().ConfigureAwait(false);
         sendCompletionLatchByMessage.RemoveOrThrow(message, completionLatch);

         Log($"Completed send to {destination.ToString("n").Substring(0, 6)} message {message}.");
      }

      private static object aLock = new object();
      private static List<Guid> guids = new List<Guid>();

      private void Log(string message, LogLevel levelElseTraceOpt = null) {
         // yes this is ghetto
         lock (aLock) {
            if (!guids.Contains(localIdentity.Id)) {
               guids.Add(localIdentity.Id);
            }

            // Console.BackgroundColor = guids.IndexOf(localIdentity.Id) == 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkCyan;
            logger.Log(levelElseTraceOpt ?? LogLevel.Trace, $"[{localIdentity.Id.ToString("n")[..6]} / {(remoteIdentity?.Id.ToString("n"))[..6]}] {message}");
            logger.Factory.Flush();
            // Console.BackgroundColor = ConsoleColor.Black;
         }
      }

      public async Task ShutdownAsync() {
         if (isShutdown) return;

         isShutdown = true;
         try {
            client.Shutdown(SocketShutdown.Both);
         } catch (SocketException) {
            // other side closed first
         }

         client.Close();
         client.Dispose();
         ns.Dispose();
         shutdownCancellationTokenSource.Cancel();
         await runAsyncInnerTask.ConfigureAwait(false);
      }
   }
}