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
using NLog;

namespace Dargon.Courier.TransportTier.Tcp.Server {
   public class TcpTransport : ITransport {
      const int kConnectionRetryIntervalMillis = 300;

      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      private readonly CancellationTokenSource shutdownCancellationTokenSource = new CancellationTokenSource();
      private readonly ConcurrentSet<TcpRoutingContext> routingContexts = new ConcurrentSet<TcpRoutingContext>();
      private readonly ConcurrentDictionary<Guid, TcpRoutingContext> clientRoutingContextsByClientId = new Commons.Collections.ConcurrentDictionary<Guid, TcpRoutingContext>();
      private readonly TcpTransportConfiguration configuration;
      private readonly Identity identity;
      private readonly RoutingTable routingTable;
      private readonly PeerTable peerTable;
      private readonly InboundMessageDispatcher inboundMessageDispatcher;
      private Task runAsyncTask;
      private Socket __listenerSocket;

      public TcpTransport(TcpTransportConfiguration configuration, Identity identity, RoutingTable routingTable, PeerTable peerTable, InboundMessageDispatcher inboundMessageDispatcher) {
         this.configuration = configuration;
         this.identity = identity;
         this.routingTable = routingTable;
         this.peerTable = peerTable;
         this.inboundMessageDispatcher = inboundMessageDispatcher;
      }

      public void Initialize() {
         runAsyncTask = RunAsync().Forgettable();
      }

      private async Task RunAsync() {
         if (configuration.Role == TcpRole.Server) {
            await RunServerAsync().ConfigureAwait(false);
         } else if (configuration.Role == TcpRole.Client) {
            await RunClientAsync().ConfigureAwait(false);
         } else {
            throw new InvalidStateException();
         }
      }

      private async Task RunServerAsync() {
         logger.Debug("Entered RunServerAsync");
         while (!shutdownCancellationTokenSource.IsCancellationRequested) {
            try {
               var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
               listener.Bind(configuration.RemoteEndpoint);
               listener.Listen(1337);
               logger.Debug($"Began listening to endpoint {configuration.RemoteEndpoint}.");

               __listenerSocket = listener;

               while (!shutdownCancellationTokenSource.IsCancellationRequested) {
                  var client = await Task.Factory.FromAsync(listener.BeginAccept, listener.EndAccept, null).ConfigureAwait(false);
                  Console.BackgroundColor = ConsoleColor.Red;
                  logger.Debug($"Got client socket from {client.RemoteEndPoint}.");
                  Console.BackgroundColor = ConsoleColor.Black;

                  var routingContext = new TcpRoutingContext(configuration, this, client, inboundMessageDispatcher, identity, routingTable, peerTable);
                  routingContexts.TryAdd(routingContext);
                  routingContext.RunAsync().Forget();
               }
            } catch (SocketException) {
               await Task.Delay(kConnectionRetryIntervalMillis);
            } catch (ObjectDisposedException) {
               // socket disposed
            }
         }
         logger.Debug("Leaving RunServerAsync");
      }

      private async Task RunClientAsync() {
         while (!shutdownCancellationTokenSource.IsCancellationRequested) {
            try {
               var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
               client.Connect(configuration.RemoteEndpoint);

               var routingContext = new TcpRoutingContext(configuration, this, client, inboundMessageDispatcher, identity, routingTable, peerTable);
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