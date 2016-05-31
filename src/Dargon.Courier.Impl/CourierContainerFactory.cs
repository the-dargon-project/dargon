using System.Threading;
using Castle.DynamicProxy;
using Dargon.Commons.Collections;
using Dargon.Courier.AsyncPrimitives;
using Dargon.Courier.ManagementTier;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;
using Dargon.Courier.ServiceTier;
using Dargon.Courier.ServiceTier.Client;
using Dargon.Courier.ServiceTier.Server;
using Dargon.Courier.ServiceTier.Vox;
using Dargon.Courier.TransportTier;
using Dargon.Ryu;
using Fody.Constructors;
using NLog;
using System.Threading.Tasks;
using Dargon.Courier.AuditingTier;
using Dargon.Courier.TransportTier.Udp.Management;

namespace Dargon.Courier {
   public class CourierBuilder {
      private readonly ConcurrentSet<ITransportFactory> transportFactories = new ConcurrentSet<ITransportFactory>();
      private readonly IRyuContainer parentContainer;

      private CourierBuilder(IRyuContainer parentContainer) {
         this.parentContainer = parentContainer;
      }

      public CourierBuilder UseTransport(ITransportFactory transportFactory) {
         this.transportFactories.TryAdd(transportFactory);
         return this;
      }

      public async Task<CourierFacade> BuildAsync() {
         var courierContainerFactory = new CourierContainerFactory(parentContainer);
         var courierContainer = await courierContainerFactory.CreateAsync(transportFactories);
         return courierContainer.GetOrThrow<CourierFacade>();
      }

      public static CourierBuilder Create() => Create(new RyuFactory().Create());
      public static CourierBuilder Create(IRyuContainer container) => new CourierBuilder(container);
   }

   [RequiredFieldsConstructor]
   public class CourierContainerFactory {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      private readonly IRyuContainer root;

      public CourierContainerFactory(IRyuContainer root) {
         this.root = root;
      }

      public async Task<IRyuContainer> CreateAsync(IReadOnlySet<ITransportFactory> transportFactories) {
         var container = root.CreateChildContainer();
         var proxyGenerator = container.GetOrDefault<ProxyGenerator>() ?? new ProxyGenerator();
         var shutdownCancellationTokenSource = new CancellationTokenSource();

         // Auditing Subsystem
         var auditService = new AuditService(shutdownCancellationTokenSource.Token);
         auditService.Initialize();

         // management tier containers
         var mobContextContainer = new MobContextContainer();
         var mobContextFactory = new MobContextFactory(auditService);
         var mobOperations = new MobOperations(mobContextFactory, mobContextContainer);

         // Other Courier Stuff
         var identity = Identity.Create();
         var routingTable = new RoutingTable();
         var peerDiscoveryEventBus = new AsyncBus<PeerDiscoveryEvent>();
         var peerTable = new PeerTable(container, table => new PeerContext(table, peerDiscoveryEventBus));

         var inboundMessageRouter = new InboundMessageRouter();
         var inboundMessageDispatcher = new InboundMessageDispatcher(identity, peerTable, inboundMessageRouter);

         var transports = new ConcurrentSet<ITransport>();
         foreach (var transportFactory in transportFactories) {
            var transport = await transportFactory.CreateAsync(mobOperations, identity, routingTable, peerTable, inboundMessageDispatcher, auditService);
            transports.TryAdd(transport);
         }

         var messenger = new Messenger(identity, transports, routingTable);

         container.Set(identity);
         container.Set(routingTable);
         container.Set(peerTable);
         container.Set(inboundMessageRouter);
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
         // Management Tier - DMI - Services
         //----------------------------------------------------------------------------------------
         var managementObjectService = new ManagementObjectService(mobContextContainer, mobOperations);
         localServiceRegistry.RegisterService<IManagementObjectService>(managementObjectService);
         container.Set(mobOperations);
         container.Set(managementObjectService);

         var facade = new CourierFacade(transports, container);
         container.Set(facade);

         return container;
      }
   }
}
