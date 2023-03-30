using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.Collections;
using Dargon.Commons.Exceptions;
using Dargon.Courier.AccessControlTier;
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
      private readonly IGatekeeper gatekeeper;
      private Task runAsyncTask;
      private Socket __listenerSocket;

      public TcpTransport(TcpTransportConfiguration configuration, Identity identity, RoutingTable routingTable, PeerTable peerTable, InboundMessageDispatcher inboundMessageDispatcher, TcpRoutingContextContainer tcpRoutingContextContainer, PayloadUtils payloadUtils, IGatekeeper gatekeeper) {
         this.Description = $"TCP Transport on {configuration.RemoteEndpoint}; {configuration.Role} ({this.GetObjectIdHash():X8})";
         this.configuration = configuration;
         this.identity = identity;
         this.routingTable = routingTable;
         this.peerTable = peerTable;
         this.inboundMessageDispatcher = inboundMessageDispatcher;
         this.tcpRoutingContextContainer = tcpRoutingContextContainer;
         this.payloadUtils = payloadUtils;
         this.gatekeeper = gatekeeper;
      }

      public string Description { get; init; }

      public void Initialize() {
         runAsyncTask = RunAsync().Forgettable();
      }

      private async Task RunAsync() {
         await Task.Yield();

         try {
            if (configuration.Role == TcpRole.Server) {
               await RunServerAsync();
            } else if (configuration.Role == TcpRole.Client) {
               await RunClientAsync();
            } else {
               throw new InvalidStateException();
            }
         } catch (Exception e) {
            logger.Error(e, $"Top level exception in {nameof(RunAsync)}");
            throw;
         }
      }

      private async Task RunServerAsync() {
         bool firstError = true;
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
                  logger.Debug($"Got client socket from {client.RemoteEndPoint}.");

                  var routingContext = new TcpRoutingContext(configuration, tcpRoutingContextContainer, client, inboundMessageDispatcher, identity, routingTable, peerTable, payloadUtils, gatekeeper);
                  tcpRoutingContextContainer.AddOrThrow(routingContext);
                  routingContext.RunAsync().Forget();
               }
            } catch (SocketException e) {
               if (firstError) {
                  if (shutdownCancellationTokenSource.IsCancellationRequested) {
                     logger.Debug(e, $"First socket error, but cancellation is requested so not an error state in {nameof(RunServerAsync)}");
                  } else {
                     logger.Error(e, $"First socket error in {nameof(RunServerAsync)}");
                  }

                  firstError = false;
               }

               await Task.Delay(kConnectionRetryIntervalMillis).ConfigureAwait(false);
            } catch (ObjectDisposedException) {
               // socket disposed
            }
         }
         logger.Debug("Leaving RunServerAsync");
      }

      private async Task RunClientAsync() {
         bool firstError = true;
         while (!shutdownCancellationTokenSource.IsCancellationRequested) {
            try {
               Console.WriteLine("Connecting to endpoint " + configuration.RemoteEndpoint);
               logger.Debug($"Connecting to endpoint {configuration.RemoteEndpoint}.");
               var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
               client.Connect(configuration.RemoteEndpoint);
               logger.Debug($"Connected to endpoint {configuration.RemoteEndpoint}.");

               var routingContext = new TcpRoutingContext(configuration, tcpRoutingContextContainer, client, inboundMessageDispatcher, identity, routingTable, peerTable, payloadUtils, gatekeeper);
               tcpRoutingContextContainer.AddOrThrow(routingContext);
               await routingContext.RunAsync();
               Console.WriteLine("Exit");
            } catch (SocketException e) {
               if (firstError) {
                  logger.Error(e, $"First socket error in {nameof(RunClientAsync)}");
                  firstError = false;
               }

               await Task.Delay(kConnectionRetryIntervalMillis);
               configuration.HandleConnectionFailure(e);
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