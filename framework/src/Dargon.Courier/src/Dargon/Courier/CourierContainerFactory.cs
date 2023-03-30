using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Collections;
using Dargon.Courier.AccessControlTier;
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
using Dargon.Courier.Vox;
using Dargon.Ryu;
using Dargon.Ryu.Modules;
using Dargon.Vox2;
using NLog;

namespace Dargon.Courier {
   public class CourierBuilder {
      private readonly ConcurrentSet<ITransportFactory> transportFactories = new ConcurrentSet<ITransportFactory>();
      private readonly IRyuContainer parentContainer;
      private VoxContext voxContext;
      private SynchronizationContext earlyIoSynchronizationContext;
      private SynchronizationContext lateIoSynchronizationContext;
      private IGatekeeper gatekeeper;
      private Guid? forceId;

      private CourierBuilder(IRyuContainer parentContainer) {
         this.parentContainer = parentContainer;
      }

      public CourierBuilder UseVoxContext(VoxContext voxContext) {
         this.voxContext = voxContext;
         return this;
      }

      public CourierBuilder UseTransport(ITransportFactory transportFactory) {
         this.transportFactories.TryAdd(transportFactory);
         return this;
      }

      public CourierBuilder UseEarlyIOSynchronizationContext(SynchronizationContext value) {
         this.earlyIoSynchronizationContext = value;
         return this;
      }

      public CourierBuilder UseGatekeeper(IGatekeeper gatekeeper) {
         this.gatekeeper = gatekeeper;
         return this;
      }

      public CourierBuilder ForceIdentity(Guid? id) {
         forceId = id;
         return this;
      }

      public async Task<CourierFacade> BuildAsync() {
         var vox = voxContext ?? VoxContextFactory.Create(new CourierVoxTypes());
         var courierContainerFactory = new CourierContainerFactory(parentContainer);
         var courierSynchronizationContexts = new CourierSynchronizationContexts {
            CourierDefault__ = DefaultThreadPoolSynchronizationContext.Instance,
            EarlyNetworkIO = earlyIoSynchronizationContext ?? DefaultThreadPoolSynchronizationContext.Instance,
            LateNetworkIO = lateIoSynchronizationContext ?? DefaultThreadPoolSynchronizationContext.Instance,
         };
         var courierContainer = await courierContainerFactory.CreateAsync(
            vox,
            courierSynchronizationContexts,
            gatekeeper,
            transportFactories,
            forceId);
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
      /// <summary>
      /// Synchronization Context used for ??
      ///
      /// Likely a "don't care" synchronization context. So far it seems to be used at initialization.
      /// </summary>
      public required SynchronizationContext CourierDefault__;

      /// <summary>
      /// Synchronization Context used for early processing of network I/O (in tandem with IOCP)
      /// It's generally important this isn't blocked by I/O, as this sync context handles acks.
      /// </summary>
      public required SynchronizationContext EarlyNetworkIO;

      /// <summary>
      /// Synchronization Context used for processing the heavy portions of network I/O, namely
      /// deserialization.
      /// </summary>
      public required SynchronizationContext LateNetworkIO;
   }

   public class CourierContainerFactory {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      private readonly IRyuContainer root;

      public CourierContainerFactory(IRyuContainer root) {
         this.root = root;
      }

      public async Task<IRyuContainer> CreateAsync(VoxContext vox, CourierSynchronizationContexts synchronizationContexts, IGatekeeper gatekeeper, IReadOnlySet<ITransportFactory> transportFactories, Guid? forceId = null) {
         transportFactories.AssertIsNotNull();
         synchronizationContexts.AssertIsNotNull();
         gatekeeper.AssertIsNotNull();

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

         // Core layers
         var identity = Identity.Create(forceId);
         container.Set(identity);

         // inbound
         var peerDiscoveryEventBus = new AsyncBus<PeerDiscoveryEvent>();
         var peerTable = new PeerTable(container, (table, peerId) => new PeerContext(table, peerId, peerDiscoveryEventBus));
         var inboundMessageRouter = new InboundMessageRouter(gatekeeper);
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
         var localServiceRegistry = new LocalServiceRegistry(identity, messenger, gatekeeper);
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
            VoxContext = vox,
            SynchronizationContexts = synchronizationContexts,
            Gatekeeper = gatekeeper,
            AuditService = auditService,
            
            Identity = identity,
            PeerTable = peerTable,
            RoutingTable = routingTable,
            
            InboundMessageRouter = inboundMessageRouter,
            InboundMessageDispatcher = inboundMessageDispatcher,
            
            Messenger = messenger,
            
            LocalServiceRegistry = localServiceRegistry,
            RemoteServiceProxyContainer = remoteServiceProxyContainer,
            
            MobOperations = mobOperations,
            ManagementObjectService = managementObjectService,
            
            Publisher = publisher,
            Subscriber = subscriber,
            PubSubClient = pubSubClient,
         };
         container.Set(facade);

         // Transports are initialized last, after all other courier objects are initialized
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