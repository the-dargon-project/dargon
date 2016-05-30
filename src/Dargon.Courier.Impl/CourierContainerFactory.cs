using System.Threading.Tasks;
using Castle.DynamicProxy;
using Dargon.Commons.Collections;
using Dargon.Courier.AsyncPrimitives;
using Dargon.Courier.ManagementTier;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;
using Dargon.Courier.ServiceTier.Client;
using Dargon.Courier.ServiceTier.Server;
using Dargon.Courier.ServiceTier.Vox;
using Dargon.Courier.TransportTier;
using Dargon.Courier.TransportTier.Udp;
using Dargon.Ryu;
using Fody.Constructors;
using NLog;

namespace Dargon.Courier {
   [RequiredFieldsConstructor]
   public class CourierContainerFactory {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      private readonly IRyuContainer root;

      public CourierContainerFactory(IRyuContainer root) {
         this.root = root;
      }

      public Task<IRyuContainer> CreateAsync() {
         return CreateAsync(new UdpTransportFactory());
      }

      public async Task<IRyuContainer> CreateAsync(ITransportFactory transportFactory) {
         var container = root.CreateChildContainer();
         var proxyGenerator = container.GetOrDefault<ProxyGenerator>() ?? new ProxyGenerator();
         
         var identity = Identity.Create();
         var routingTable = new RoutingTable();
         var peerDiscoveryEventBus = new AsyncBus<PeerDiscoveryEvent>();
         var peerTable = new PeerTable(container, table => new PeerContext(table, peerDiscoveryEventBus));

         var inboundMessageRouter = new InboundMessageRouter();
         var inboundMessageDispatcher = new InboundMessageDispatcher(identity, peerTable, inboundMessageRouter);

         var transport = await transportFactory.CreateAsync(identity, routingTable, peerTable, inboundMessageDispatcher);
         var transports = new ConcurrentSet<ITransport>();
         transports.TryAdd(transport);

         var facade = new CourierFacade(transports);
         var messenger = new Messenger(identity, transports, routingTable);

         container.Set(identity);
         container.Set(routingTable);
         container.Set(peerTable);
         container.Set(inboundMessageRouter);
         container.Set(facade);
         container.Set(messenger);

         //----------------------------------------------------------------------------------------
         // Service Tier - Service Discovery, Remote Method Invocation
         //----------------------------------------------------------------------------------------
         var localServiceRegistry = new LocalServiceRegistry(messenger);
         var remoteServiceInvoker = new RemoteServiceInvoker(messenger);
         var remoteServiceProxyContainer = new RemoteServiceProxyContainer(proxyGenerator, remoteServiceInvoker);
         inboundMessageRouter.RegisterHandler<RmiRequestDto>(localServiceRegistry.HandleInvocationRequestAsync);
         inboundMessageRouter.RegisterHandler<RmiResponseDto>(remoteServiceInvoker.HandleInvocationResponse);
         container.Set(localServiceRegistry);
         container.Set(remoteServiceProxyContainer);

         //----------------------------------------------------------------------------------------
         // Management Tier - DMI
         //----------------------------------------------------------------------------------------
         var managementObjectService = new ManagementObjectService();
         container.Set(managementObjectService);
         return container;
      }
   }
}
