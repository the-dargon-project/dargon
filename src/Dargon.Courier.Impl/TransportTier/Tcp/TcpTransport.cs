using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.Collections;
using Dargon.Commons.Exceptions;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;
using Dargon.Courier.Vox;

namespace Dargon.Courier.TransportTier.Tcp.Server {
   public class TcpTransport : ITransport {
      const int kConnectionRetryIntervalMillis = 300;

      private readonly CancellationTokenSource shutdownCancellationTokenSource = new CancellationTokenSource();
      private readonly ConcurrentSet<TcpRoutingContext> routingContexts = new ConcurrentSet<TcpRoutingContext>();
      private readonly ConcurrentDictionary<Guid, TcpRoutingContext> clientRoutingContextsByClientId = new Commons.Collections.ConcurrentDictionary<Guid, TcpRoutingContext>();
      private readonly IPEndPoint remoteEndpoint;
      private readonly TcpRole role;
      private readonly Identity identity;
      private readonly RoutingTable routingTable;
      private readonly PeerTable peerTable;
      private readonly InboundMessageDispatcher inboundMessageDispatcher;
      private Task runAsyncTask;
      private Socket __listenerSocket;

      public TcpTransport(IPEndPoint remoteEndpoint, TcpRole role, Identity identity, RoutingTable routingTable, PeerTable peerTable, InboundMessageDispatcher inboundMessageDispatcher) {
         this.remoteEndpoint = remoteEndpoint;
         this.role = role;
         this.identity = identity;
         this.routingTable = routingTable;
         this.peerTable = peerTable;
         this.inboundMessageDispatcher = inboundMessageDispatcher;
      }

      public void Initialize() {
         runAsyncTask = RunAsync().Forgettable();
      }

      private async Task RunAsync() {
         if (role == TcpRole.Server) {
            await RunServerAsync().ConfigureAwait(false);
         } else if (role == TcpRole.Client) {
            await RunClientAsync().ConfigureAwait(false);
         } else {
            throw new InvalidStateException();
         }
      }

      private async Task RunServerAsync() {
         while (!shutdownCancellationTokenSource.IsCancellationRequested) {
            try {
               var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
               listener.Bind(remoteEndpoint);
               listener.Listen(1337);

               __listenerSocket = listener;

               while (!shutdownCancellationTokenSource.IsCancellationRequested) {
                  var client = await Task.Factory.FromAsync(listener.BeginAccept, listener.EndAccept, null).ConfigureAwait(false);
                  var routingContext = new TcpRoutingContext(this, client, inboundMessageDispatcher, identity, routingTable, peerTable);
                  routingContexts.TryAdd(routingContext);
                  routingContext.RunAsync().Forget();
               }
            } catch (SocketException) {
               await Task.Delay(kConnectionRetryIntervalMillis);
            } catch (ObjectDisposedException) {
               // socket disposed
            }
         }
      }

      private async Task RunClientAsync() {
         while (!shutdownCancellationTokenSource.IsCancellationRequested) {
            try {
               var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
               client.Connect(remoteEndpoint);

               var routingContext = new TcpRoutingContext(this, client, inboundMessageDispatcher, identity, routingTable, peerTable);
               routingContexts.TryAdd(routingContext);
               await routingContext.RunAsync().ConfigureAwait(false);
            } catch (SocketException) {
               await Task.Delay(kConnectionRetryIntervalMillis);
            }
         }
      }

      public void SomethingToDoWithRoutingAndStarting(Guid clientId, TcpRoutingContext routingContext) {
         clientRoutingContextsByClientId.AddOrThrow(clientId, routingContext);
      }

      public void SomethingToDoWithRoutingAndEnding(Guid clientId, TcpRoutingContext routingContext) {
         clientRoutingContextsByClientId.RemoveOrThrow(clientId, routingContext);
      }

      public Task SendMessageBroadcastAsync(MessageDto message) {
         return Task.WhenAll(
            clientRoutingContextsByClientId.Values.Select(
               clientRoutingContext => clientRoutingContext.SendBroadcastAsync(message)));
      }

      public async Task SendMessageReliableAsync(Guid destination, MessageDto message) {
         TcpRoutingContext clientRoutingContext;
         if (clientRoutingContextsByClientId.TryGetValue(destination, out clientRoutingContext)) {
            await clientRoutingContext.SendReliableAsync(destination, message);
         }
      }

      public async Task SendMessageUnreliableAsync(Guid destination, MessageDto message) {
         TcpRoutingContext clientRoutingContext;
         if (clientRoutingContextsByClientId.TryGetValue(destination, out clientRoutingContext)) {
            await clientRoutingContext.SendUnreliableAsync(destination, message);
         }
      }

      public async Task ShutdownAsync() {
         shutdownCancellationTokenSource.Cancel();
         __listenerSocket?.Close();
         __listenerSocket?.Dispose();
         await Task.WhenAll(routingContexts.Select(rc => rc.ShutdownAsync()));
         await runAsyncTask;
      }
   }
}