using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.Collections;
using Dargon.Commons.Exceptions;
using Dargon.Courier.AuditingTier;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;
using Dargon.Courier.Vox;
using NLog;

namespace Dargon.Courier.TransportTier.Tcp.Server {
   public class TcpTransport : ITransport {
      const int kConnectionRetryIntervalMillis = 300;

      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      private readonly CancellationTokenSource shutdownCancellationTokenSource = new CancellationTokenSource();
      private readonly TcpTransportConfiguration configuration;
      private readonly Identity identity;
      private readonly RoutingTable routingTable;
      private readonly PeerTable peerTable;
      private readonly InboundMessageDispatcher inboundMessageDispatcher;
      private readonly TcpRoutingContextContainer tcpRoutingContextContainer;
      private readonly PayloadUtils payloadUtils;
      private Task runAsyncTask;
      private Socket __listenerSocket;

      public TcpTransport(TcpTransportConfiguration configuration, Identity identity, RoutingTable routingTable, PeerTable peerTable, InboundMessageDispatcher inboundMessageDispatcher, TcpRoutingContextContainer tcpRoutingContextContainer, PayloadUtils payloadUtils) {
         this.configuration = configuration;
         this.identity = identity;
         this.routingTable = routingTable;
         this.peerTable = peerTable;
         this.inboundMessageDispatcher = inboundMessageDispatcher;
         this.tcpRoutingContextContainer = tcpRoutingContextContainer;
         this.payloadUtils = payloadUtils;
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

                  var routingContext = new TcpRoutingContext(configuration, tcpRoutingContextContainer, client, inboundMessageDispatcher, identity, routingTable, peerTable, payloadUtils);
                  tcpRoutingContextContainer.AddOrThrow(routingContext);
                  routingContext.RunAsync().Forget();
               }
            } catch (SocketException) {
               await Task.Delay(kConnectionRetryIntervalMillis).ConfigureAwait(false);
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

               var routingContext = new TcpRoutingContext(configuration, tcpRoutingContextContainer, client, inboundMessageDispatcher, identity, routingTable, peerTable, payloadUtils);
               tcpRoutingContextContainer.AddOrThrow(routingContext);
               await routingContext.RunAsync().ConfigureAwait(false);
            } catch (SocketException) {
               await Task.Delay(kConnectionRetryIntervalMillis).ConfigureAwait(false);
            }
         }
      }

      public Task SendMessageBroadcastAsync(MessageDto message) {
         return Task.WhenAll(
            tcpRoutingContextContainer.Enumerate().Select(
               clientRoutingContext => clientRoutingContext.SendBroadcastAsync(message)));
      }

      public async Task ShutdownAsync() {
         shutdownCancellationTokenSource.Cancel();
         __listenerSocket?.Close();
         __listenerSocket?.Dispose();
         await tcpRoutingContextContainer.ShutdownAsync().ConfigureAwait(false);
         await runAsyncTask.ConfigureAwait(false);
      }
   }
}