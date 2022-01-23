﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Exceptions;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;
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
      private readonly RoutingTable routingTable;
      private Identity remoteIdentity;
      private bool isHandshakeComplete = false;
      private Task runAsyncInnerTask;
      private volatile bool isShutdown = false;

      public TcpRoutingContext(TcpTransportConfiguration configuration, TcpRoutingContextContainer tcpRoutingContextContainer, Socket client, InboundMessageDispatcher inboundMessageDispatcher, Identity localIdentity, RoutingTable routingTable, PeerTable peerTable, PayloadUtils payloadUtils) {
         logger.Debug($"Constructing TcpRoutingContext for client {client.RemoteEndPoint}, localId: {localIdentity}");

         this.configuration = configuration;
         this.tcpRoutingContextContainer = tcpRoutingContextContainer;
         this.client = client;
         this.inboundMessageDispatcher = inboundMessageDispatcher;
         this.localIdentity = localIdentity;
         this.routingTable = routingTable;
         this.peerTable = peerTable;
         this.payloadUtils = payloadUtils;
         this.ns = new NetworkStream(client);
      }

      public async Task RunAsync() {
         Log($"Entered RunAsync.");
         runAsyncInnerTask = RunAsyncHelper();
         await runAsyncInnerTask.ConfigureAwait(false);
         Log($"Left RunAsync.");
      }

      private async Task RunAsyncHelper() {
         bool isRemoteIdentityAssociated = false;
         bool isRoutingTableRouteRegistered = false;
         try {
            var shutdownCtsTask = shutdownCancellationTokenSource.Token.AsTask();
            var sendAndRecvHandshakesTask = Task.WhenAll(
               Go(async () => {
                  var remoteToLocalHandshake = (HandshakeDto)await payloadUtils.ReadPayloadAsync(ns, shutdownCancellationTokenSource.Token).ConfigureAwait(false);
                  remoteIdentity = remoteToLocalHandshake.Identity;
               }),
               Go(async () => {
                  var localToRemoteHandshake = new HandshakeDto { Identity = localIdentity };
                  await payloadUtils.WritePayloadAsync(ns, localToRemoteHandshake, writerLock, shutdownCancellationTokenSource.Token).ConfigureAwait(false);
               })
            );
            await Task.WhenAny(shutdownCtsTask, sendAndRecvHandshakesTask).ConfigureAwait(false);

            if (shutdownCancellationTokenSource.IsCancellationRequested) {
               return;
            }

            if (sendAndRecvHandshakesTask.IsFaulted) {
               await sendAndRecvHandshakesTask;
            }

            isHandshakeComplete = true;

            var readerTask = RunReaderAsync(shutdownCancellationTokenSource.Token).Forgettable();
            var writerTask = RunWriterAsync(shutdownCancellationTokenSource.Token).Forgettable();

            tcpRoutingContextContainer.AssociateRemoteIdentityOrThrow(remoteIdentity.Id, this);
            isRemoteIdentityAssociated = true;
            routingTable.Register(remoteIdentity.Id, this);
            isRoutingTableRouteRegistered = true;

            peerTable.GetOrAdd(remoteIdentity.Id).HandleInboundPeerIdentityUpdate(remoteIdentity);

            configuration.HandleRemoteHandshakeCompletion(remoteIdentity);

            try {
               await Task.WhenAny(
                  readerTask,
                  writerTask,
                  shutdownCtsTask

               ).ConfigureAwait(false);
               shutdownCancellationTokenSource.Cancel();
            } catch (OperationCanceledException) when (isShutdown) { }
         } catch (Exception e) {
            Log($"RunAsync threw {e}", LogLevel.Error);
         } finally {
            if (isRoutingTableRouteRegistered) {
               routingTable.Unregister(remoteIdentity.Id, this);
            }

            if (isRemoteIdentityAssociated) {
               tcpRoutingContextContainer.UnassociateRemoteIdentityOrThrow(remoteIdentity.Id, this);
            }

            tcpRoutingContextContainer.RemoveOrThrow(this);
         }
      }

      private async Task RunReaderAsync(CancellationToken token) {
         Log($"Entered RunReaderAsync.");
         try {
            while (!isShutdown) {
               var payload = await payloadUtils.ReadPayloadAsync(ns, token).ConfigureAwait(false);
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
               await outboundMessageSignal.WaitAsync(token).ConfigureAwait(false);
               Go(async () => {
                  Log($"Entered message writer task.");
                  MessageDto message;
                  if (!outboundMessageQueue.TryDequeue(out message)) {
                     throw new InvalidStateException();
                  }

                  Log($"Writing message {message} destination {message.ReceiverId.ToString("n").Substring(0, 6)}.");
                  await payloadUtils.WritePayloadAsync(ns, message, writerLock, token).ConfigureAwait(false);
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
         if (!isHandshakeComplete || !remoteIdentity.Matches(destination, IdentityMatchingScope.Broadcast)) {
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
            logger.Log(levelElseTraceOpt ?? LogLevel.Trace, $"[{localIdentity.Id.ToString("n").Substring(0, 6)} / {isHandshakeComplete}] {message}");
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