using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Collections;
using Dargon.Courier.AuditingTier;
using Dargon.Courier.ManagementTier;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.PubSubTier;
using Dargon.Courier.PubSubTier.Publishers;
using Dargon.Courier.PubSubTier.Subscribers;
using Dargon.Courier.RoutingTier;
using Dargon.Courier.ServiceTier;
using Dargon.Courier.ServiceTier.Client;
using Dargon.Courier.ServiceTier.Server;
using Dargon.Courier.ServiceTier.Vox;
using Dargon.Courier.TransportTier;
using Dargon.Ryu;
using Dargon.Ryu.Modules;
using NLog;

namespace Dargon.Courier {
   public class CourierBuilder {
      private readonly ConcurrentSet<ITransportFactory> transportFactories = new ConcurrentSet<ITransportFactory>();
      private readonly IRyuContainer parentContainer;
      private SynchronizationContext synchronizationContext;
      private Guid? forceId;

      private CourierBuilder(IRyuContainer parentContainer) {
         this.parentContainer = parentContainer;
      }

      public CourierBuilder UseTransport(ITransportFactory transportFactory) {
         this.transportFactories.TryAdd(transportFactory);
         return this;
      }

      public CourierBuilder UseSynchronizationContext(SynchronizationContext synchronizationContext) {
         this.synchronizationContext = synchronizationContext;
         return this;
      }

      public CourierBuilder ForceIdentity(Guid? id) {
         forceId = id;
         return this;
      }

      public async Task<CourierFacade> BuildAsync() {
         var courierContainerFactory = new CourierContainerFactory(parentContainer);
         var courierSynchronizationContexts = new CourierSynchronizationContexts {
            CourierDefault = synchronizationContext ?? DefaultThreadPoolSynchronizationContext.Instance,
         };
         var courierContainer = await courierContainerFactory.CreateAsync(transportFactories, courierSynchronizationContexts, forceId).ConfigureAwait(false);
         return courierContainer.GetOrThrow<CourierFacade>();
      }

      public static CourierBuilder Create() {
         return Create(new RyuFactory().Create());
      }

      public static CourierBuilder Create(IRyuContainer container) {
         return new CourierBuilder(container);
      }
   }

   public class CourierSynchronizationContexts {
      public SynchronizationContext CourierDefault;
   }

   public class CourierContainerFactory {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      private readonly IRyuContainer root;

      public CourierContainerFactory(IRyuContainer root) {
         this.root = root;
      }

      public async Task<IRyuContainer> CreateAsync(IReadOnlySet<ITransportFactory> transportFactories, CourierSynchronizationContexts synchronizationContexts, Guid? forceId = null) {
         var container = root.CreateChildContainer();
         container.Set(synchronizationContexts);

         var proxyGenerator = container.GetOrDefault<ProxyGenerator>() ?? new ProxyGenerator();
         var shutdownCancellationTokenSource = new CancellationTokenSource();

         // Auditing Tier Containers
         var auditService = new AuditService(shutdownCancellationTokenSource.Token);
         auditService.Initialize();

         // Management Tier Containers (depended upon by core systems)
         var mobContextContainer = new MobContextContainer();
         var mobContextFactory = new MobContextFactory(auditService);
         var mobOperations = new MobOperations(mobContextFactory, mobContextContainer);
         container.Set(mobOperations);
         // var courierContainerMobNamespace = "Dargon.Courier.Instances." + this.GetObjectIdHash().ToString("X8");

         // Core layers
         var identity = Identity.Create(forceId);
         container.Set(identity);

         // inbound
         var peerDiscoveryEventBus = new AsyncBus<PeerDiscoveryEvent>();
         var peerTable = new PeerTable(container, (table, peerId) => new PeerContext(table, peerId, peerDiscoveryEventBus));
         var inboundMessageRouter = new InboundMessageRouter();
         var inboundMessageDispatcher = new InboundMessageDispatcher(identity, peerTable, inboundMessageRouter);
         container.Set(peerTable);
         container.Set(inboundMessageRouter);

         // outbound
         var routingTable = new RoutingTable();
         container.Set(routingTable);

         // transports - initially null, added to later.
         var transports = new ConcurrentSet<ITransport>();

         // messenger
         var messenger = new Messenger(identity, transports, routingTable);
         container.Set(messenger);

         //----------------------------------------------------------------------------------------
         // Service Tier - Service Discovery, Remote Method Invocation
         //----------------------------------------------------------------------------------------
         var localServiceRegistry = new LocalServiceRegistry(identity, messenger);
         var remoteServiceInvoker = new RemoteServiceInvoker(identity, messenger);
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
         container.Set(managementObjectService);

         //----------------------------------------------------------------------------------------
         // PubSub Tier
         //----------------------------------------------------------------------------------------
         var localTopicsTable = new LocalTopicsTable();
         var publisher = new Publisher(messenger, localTopicsTable);
         var subscriber = new Subscriber(inboundMessageRouter, remoteServiceProxyContainer);
         var pubSubClient = new PubSubClient(publisher, subscriber);
         container.Set(publisher);
         container.Set(subscriber);
         container.Set(pubSubClient);

         var pubSubService = new PubSubService(localTopicsTable);
         localServiceRegistry.RegisterService<IPubSubService>(pubSubService);
         container.Set(pubSubService);

         // Courier Facade
         var facade = new CourierFacade(transports, container) {
            SynchronizationContexts = synchronizationContexts,
            AuditService = auditService,
            MobOperations = mobOperations,
            Identity = identity,
            InboundMessageRouter = inboundMessageRouter,
            InboundMessageDispatcher = inboundMessageDispatcher,
            PeerTable = peerTable,
            RoutingTable = routingTable,
            Messenger = messenger,
            LocalServiceRegistry = localServiceRegistry,
            RemoteServiceProxyContainer = remoteServiceProxyContainer,
            ManagementObjectService = managementObjectService,
            Publisher = publisher,
            Subscriber = subscriber,
            PubSubClient = pubSubClient,
         };
         container.Set(facade);

         foreach (var transportFactory in transportFactories) {
            await facade.AddTransportAsync(transportFactory);
         }

         return container;
      }
   }

   public class CourierRyuModule : RyuModule {
      public override RyuModuleFlags Flags => RyuModuleFlags.Default;

      public CourierRyuModule() {
         OptionalSingleton(c => c.SynchronizationContexts);
         OptionalSingleton(c => c.Identity);
         OptionalSingleton(c => c.InboundMessageRouter);
         OptionalSingleton(c => c.PeerTable);
         OptionalSingleton(c => c.RoutingTable);
         OptionalSingleton(c => c.Messenger);
         OptionalSingleton(c => c.LocalServiceRegistry);
         OptionalSingleton(c => c.RemoteServiceProxyContainer);
         OptionalSingleton(c => c.MobOperations);
         OptionalSingleton(c => c.ManagementObjectService);
         OptionalSingleton(c => c.Publisher);
         OptionalSingleton(c => c.Subscriber);
         OptionalSingleton(c => c.PubSubClient);
      }

      private void OptionalSingleton<T>(Func<CourierFacade, T> cb) {
         Optional.Singleton(ryu => cb(ryu.GetOrActivate<CourierFacade>()));
      }
   }
}