using Dargon.Commons;
using Dargon.Commons.Channels;
using Dargon.Commons.Collections;
using Dargon.Commons.Exceptions;
using Dargon.Courier;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.ServiceTier;
using Dargon.Courier.ServiceTier.Client;
using Dargon.Courier.TransportTier.Tcp;
using Dargon.Courier.TransportTier.Udp;
using Dargon.Courier.Vox;
using Dargon.Hydrous.Impl.BinaryLogNamespaceThing;
using Dargon.Ryu;
using Dargon.Vox;
using Fody.Constructors;
using Nito.AsyncEx;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Courier.AsyncPrimitives;
using Dargon.Vox.Utilities;
using static Dargon.Commons.Channels.ChannelsExtensions;
using SCG = System.Collections.Generic;

namespace Dargon.Hydrous.Impl {
   public class Program {
      public static void Main(string[] args) {
         Console.BufferHeight = 21337;
         InitializeLogging();
         CacheRoot<int, string>.StartLocalCluster();
      }

      private static void InitializeLogging() {
         var config = new LoggingConfiguration();
         Target debuggerTarget = new DebuggerTarget() {
            Layout = "${longdate}|${level}|${logger}|${message} ${exception:format=tostring}"
         };
         Target consoleTarget = new ColoredConsoleTarget() {
            Layout = "${longdate}|${level}|${logger}|${message} ${exception:format=tostring}"
         };

#if !DEBUG
         debuggerTarget = new AsyncTargetWrapper(debuggerTarget);
         consoleTarget = new AsyncTargetWrapper(consoleTarget);
#else
         new AsyncTargetWrapper().Wrap(); // Placeholder for optimizing imports
#endif

         config.AddTarget("debugger", debuggerTarget);
         config.AddTarget("console", consoleTarget);

         var debuggerRule = new LoggingRule("*", LogLevel.Debug, debuggerTarget);
         config.LoggingRules.Add(debuggerRule);

         var consoleRule = new LoggingRule("*", LogLevel.Debug, consoleTarget);
         config.LoggingRules.Add(consoleRule);

         LogManager.Configuration = config;
      }
   }


   public class HydarVoxTypes : VoxTypes {
      public HydarVoxTypes() : base(50) {
         Register<ElectDto>(0);
         Register<LeaderHeartBeatDto>(1);
         Register<RepartitionCompleteDto>(2);
         Register<BinaryLogEntry>(3);
      }
   }

   [AutoSerializable]
   public class ElectDto {
      public Guid NomineeId { get; set; }
      public IReadOnlySet<Guid> FollowerIds { get; set; }
   }

   [AutoSerializable]
   public class LeaderHeartBeatDto {
      public IReadOnlySet<Guid> CohortIds { get; set; }
   }

   [AutoSerializable]
   public class RepartitionHaltDto { }

   [AutoSerializable]
   public class RepartitionCompleteDto {
      public RepartitionCompleteDto() { }

      public RepartitionCompleteDto(Guid[] rankedCohortIds, SCG.IReadOnlyDictionary<int, IReadOnlySet<int>> partitionIdsByRank) {
         RankedCohortIds = rankedCohortIds;
         PartitionIdsByRank = partitionIdsByRank;
      }

      public SCG.IReadOnlyList<Guid> RankedCohortIds { get; set; }
      public SCG.IReadOnlyDictionary<int, IReadOnlySet<int>> PartitionIdsByRank { get; set; }
   }

   [AutoSerializable]
   public class RequestDto<K, V> {
      public IEntryOperation<K, V> Operation { get; set; }
   }

   [AutoSerializable]
   public class HaveDto {
      public int EpochId { get; set; }
      public int BlockId { get; set; }
   }

   [AutoSerializable]
   public class EntryUpdateDto { }

   [AutoSerializable]
   public class EntryDto<K, V> {
      public K Key { get; set; }
      public V Value { get; set; }
      public bool Exists { get; set; }
   }

   [AutoSerializable]
   public class SOmethingToDoWithHeyIhaveThisTHing {
      public int Progress { get; set; }
   }

   [AutoSerializable]
   public class SomethingToDoWithHeyWeHaveUpdatesHereYouGo {
      public int Progress { get; set; }
   }

   [AutoSerializable]
   public class SomethingToDoWithHeyINeedThisUpdateKthxDto {
      public int Progress { get; set; }
   }

   [AutoSerializable]
   public class SomethingToDoWithReplicationCompletionDto {
      public int Progress { get; set; }
   }

   public class CacheRoot<K, V> {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      public static void StartLocalCluster() {
         int port = 21337;

         Func<CourierFacade> buildCourier = () => CourierBuilder.Create()
                                                                .UseUdpMulticastTransport()
                                                                .UseTcpServerTransport(port++)
                                                                .BuildAsync().Result;
         StartNewCohort(buildCourier);
//         StartNewCohort(buildCourier);
//         StartNewCohort(buildCourier);
//         StartNewCohort(buildCourier);
         new CountdownEvent(1).Wait();
      }

      public static void StartNewCohort(Func<CourierFacade> buildCourier) {
         var root = new RyuFactory().Create();
         var courier = buildCourier();
         var identity = courier.Identity;
         var router = courier.InboundMessageRouter;
         string cacheName = "my-cache";
         var cacheNameMd5 = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(cacheName));
         var cacheGuid = new Guid(cacheNameMd5.SubArray(0, 16));
         var staticConfiguration = new StaticConfiguration { CacheId = cacheGuid, LocalId = identity.Id };
         var liveConfiguration = new LiveConfiguration();
         var partitioner = new Partitioner(staticConfiguration, liveConfiguration);

         var clusterMessenger = new ClusterMessenger(courier);

         var slaveBinaryLogContainer = new SlaveBinaryLogContainer();
         var something = new Something();
         var inboundExecutionContextChannel = something.GetChannelOfCacheId(cacheGuid);

         var phaseContext = new PhaseContext("MAIN", null, cacheGuid, courier, clusterMessenger, staticConfiguration, liveConfiguration, slaveBinaryLogContainer, inboundExecutionContextChannel, partitioner);
         phaseContext.TransitionAsync(new IndeterminatePhase()).Forget();

         router.RegisterHandler<ElectDto>(SomeCloningProxy<ElectDto>(phaseContext));
         router.RegisterHandler<LeaderHeartBeatDto>(SomeCloningProxy<LeaderHeartBeatDto>(phaseContext));
         router.RegisterHandler<RepartitionHaltDto>(SomeCloningProxy<RepartitionHaltDto>(phaseContext));
         router.RegisterHandler<RepartitionCompleteDto>(SomeCloningProxy<RepartitionCompleteDto>(phaseContext));
         router.RegisterHandler<RequestDto<K, V>>(SomeCloningProxy<RequestDto<K, V>>(phaseContext));
         router.RegisterHandler<EntryUpdateDto>(SomeCloningProxy<EntryUpdateDto>(phaseContext));

         var someRepartitioningService = new SomeRepartitioningService();
         courier.LocalServiceRegistry.RegisterService<ISomeRepartitioningService>(someRepartitioningService);

         var someReplicationService = new SomeReplicationService(slaveBinaryLogContainer);
         courier.LocalServiceRegistry.RegisterService<ISomeReplicationService>(someReplicationService);

         var cacheService = new CacheService<K,V>(something);
         courier.LocalServiceRegistry.RegisterService<ICacheService<K, V>>(cacheGuid, cacheService);
      }

      public static Func<IInboundMessageEvent<T>, Task> SomeCloningProxy<T>(PhaseContext phaseContext) {
         return async x => {
            await Task.Yield();

            var clone = new InboundMessageEvent<T>();
            clone.Message = x.Message;
            clone.Sender = x.Sender;

            phaseContext.ProcessInboundMessageAsync(clone).Forget();
         };
      }

      public class StaticConfiguration {
         /// <summary>
         /// Unique id based on cache name
         /// </summary>
         public Guid CacheId { get; set; }

         /// <summary>
         /// Default: 10 to get 2^10 Blocks.
         /// The top BlockCountPower bits of a key's hash determine
         /// its blockId.
         /// </summary>
         public int BlockCountPower { get; set; } = 10;

         /// <summary>
         /// Derived number of blocks
         /// </summary>
         public int DerivedBlockCount => 1 << BlockCountPower;

         /// <summary>
         /// Replication Factor.
         /// Default: 2: Entries are mirrored on two cohorts at a time.
         /// </summary>
         public int Redundancy { get; set; } = 2;

         /// <summary>
         /// Identity of local node - determined by courier identity
         /// </summary>
         public Guid LocalId { get; set; }
      }

      public class LiveConfiguration {
         public int LocalRank { get; set; }
         public Guid[] CohortIdsByRank { get; set; }
         public int CohortCount => CohortIdsByRank.Length;
         public int PartitionCount => CohortCount;
         public SCG.IReadOnlyDictionary<int, IReadOnlySet<int>> PartitionsIdsByRank { get; set; }
      }

      public interface ICache {
         V Get(K key);
         void Put(K key, V value);
      }

      public class LocalCache : ICache {
         private readonly ConcurrentDictionary<K, V> inner = new ConcurrentDictionary<K, V>();
         public V Get(K key) => inner[key];
         public void Put(K key, V value) => inner[key] = value;
      }

      public enum EntryOperationType {
         Read,
         Put
      }

      public interface IEntryOperation {
         K Key { get; }
         EntryOperationType Type { get; }
      }

      public interface IEntryOperation<TResult> : IEntryOperation {
         Task<TResult> ExecuteAsync(Entry entry);
      }

      public class EntryReadOperation : IEntryOperation<Entry> {
         public K Key { get; set; }
         public EntryOperationType Type => EntryOperationType.Read;

         public Task<Entry> ExecuteAsync(Entry entry) {
            return Task.FromResult(entry);
         }
      }

      public class EntryPutOperation : IEntryOperation<Entry> {
         public K Key { get; set; }
         public V Value { get; set; }
         public EntryOperationType Type => EntryOperationType.Put;
         public Task<Entry> ExecuteAsync(Entry entry) {
            var oldEntry = entry.DeepCloneSerializable();
            entry.Value = Value;
            return Task.FromResult(oldEntry);
         }
      }

      public interface IEntryOperationExecutionContext {
         IEntryOperation Operation { get; }
         Task ExecuteAsync(Entry entry);
      }

      public class EntryOperationExecutionContext<TResult> : IEntryOperationExecutionContext {
         public AsyncBox<TResult> ResultBox { get; } = new AsyncBox<TResult>();
         IEntryOperation IEntryOperationExecutionContext.Operation => Operation;
         public IEntryOperation<TResult> Operation { get; set; }

         public async Task ExecuteAsync(Entry entry) {
            var result = await Operation.ExecuteAsync(entry);
            ResultBox.SetResult(result);
         }
      }

      [AutoSerializable]
      public class Entry {
         private V value;

         public K Key { get; private set; }
         public V Value { get { return value; } set { SetValue(value); } }
         public bool Exists { get; private set; }
         public bool IsDirty { get; set; }

         private void SetValue(V newValue) {
            value = newValue;
            IsDirty = true;
            Exists = true;
         }

         public static Entry Create(K key) => new Entry { Key = key };
      }

      public class Something {
         private readonly ConcurrentDictionary<Guid, Channel<IEntryOperationExecutionContext>> executionContextChannelByCacheId = new ConcurrentDictionary<Guid, Channel<IEntryOperationExecutionContext>>();

         public Task SomethingToDoWithAddingAExecutionContextToAChannel(Guid cacheId, IEntryOperationExecutionContext executionContext) {
            return GetChannelOfCacheId(cacheId).WriteAsync(executionContext);
         }

         public Channel<IEntryOperationExecutionContext> GetChannelOfCacheId(Guid cacheId) {
            return executionContextChannelByCacheId.GetOrAdd(
               cacheId,
               add => ChannelFactory.Nonblocking<IEntryOperationExecutionContext>());
         }
      }

      public class SomethingToDoWithEntryOperationChuggingAbstractBeanFactorySingleton {
         private readonly AsyncAutoResetEvent operationAvailableSignal = new AsyncAutoResetEvent(false);
         private readonly ConcurrentQueue<IEntryOperationExecutionContext> readOperationExecutionContextQueue = new ConcurrentQueue<IEntryOperationExecutionContext>();
         private readonly ConcurrentQueue<IEntryOperationExecutionContext> putOperationExecutionContextQueue = new ConcurrentQueue<IEntryOperationExecutionContext>();
         
         public async Task RunAsync() {
            var entry = new Entry();
            while (true) {
               await operationAvailableSignal.WaitAsync();
               ProcessReadsAsync(entry.DeepCloneSerializable()).Forget();
               await ProcessPutsAsync(entry);
            }
         }

         private async Task ProcessReadsAsync(Entry entry) {
            IEntryOperationExecutionContext executionContext;
            var readExecutionContexts = new SCG.List<IEntryOperationExecutionContext>();
            while (readOperationExecutionContextQueue.TryDequeue(out executionContext)) {
               readExecutionContexts.Add(executionContext);
            }
            await Task.WhenAll(readExecutionContexts.Map(ec => ec.ExecuteAsync(entry)));
         }

         private async Task ProcessPutsAsync(Entry entry) {
            IEntryOperationExecutionContext executionContext;
            while (putOperationExecutionContextQueue.TryDequeue(out executionContext)) {
               await executionContext.ExecuteAsync(entry);
            }
         }

         public Task<TResult> EnqueueOperationAndGetResultAsync<TResult>(IEntryOperation<TResult> entryOperation) {
            var executionContext = new EntryOperationExecutionContext<TResult> {
               Operation = entryOperation
            };
            switch (executionContext.Operation.Type) {
               case EntryOperationType.Read:
                  readOperationExecutionContextQueue.Enqueue(executionContext);
                  break;
               case EntryOperationType.Put:
                  putOperationExecutionContextQueue.Enqueue(executionContext);
                  break;
            }
            operationAvailableSignal.Set();
            return executionContext.ResultBox.GetResultAsync();
         }
      }

      public class CacheRequestContext {

      }

      public interface IMessenger {
         Task SendToCluster<T>(T val);
         Task SendToCohortReliable<T>(Guid dest, T val);
         Task SendToCohortUnreliable<T>(Guid dest, T val);
      }

      public abstract class PhaseBase {
         public bool IsRunning { get; set; }
         public PhaseBase LastPhase { get; set; }
         public int Generation { get; set; }
         public abstract string Description { get; }

         public virtual Task HandleEnterAsync() => Task.FromResult(false);
         public abstract Task RunAsync();
         public virtual Task HandleLeaveAsync() => Task.FromResult(false);

         public Task TransitionAsync(PhaseBase nextPhase) => Context.TransitionAsync(nextPhase);

         public Task FailInvalidState() {
            throw new InvalidStateException();
         }

         public Task FailNotImplemented() {
            throw new NotImplementedException();
         }

         public PhaseContext Context { get; set; }
         public Guid CacheId => Context.CacheId;
         public CourierFacade Courier => Context.Courier;
         public ClusterMessenger Messenger => Context.Messenger;
         public Identity Identity => Courier.Identity;
         public PeerTable PeerTable => Courier.PeerTable;
         public RemoteServiceProxyContainer RemoteServiceProxyContainer => Courier.RemoteServiceProxyContainer;
         public PhaseContextChannels Channels => Context.Channels;
         public StaticConfiguration StaticConfiguration => Context.StaticConfiguration;
         public LiveConfiguration LiveConfiguration => Context.LiveConfiguration;
         public SlaveBinaryLogContainer SlaveBinaryLogContainer => Context.SlaveBinaryLogContainer;
         public Partitioner Partitioner => Context.Partitioner;


         public void Log(string s) => Context.Log(s);
      }

      private static object consoleLogLock = new object();

      public class PhaseContext {
         private static readonly Logger logger = LogManager.GetCurrentClassLogger();

         private readonly ConcurrentSet<PhaseContext> childContexts = new ConcurrentSet<PhaseContext>();
         private readonly string contextName;
         private readonly PhaseContext parentPhaseContext;
         private PhaseBase currentPhase = null;
         private int generationCounter = 0;

         public PhaseContext(string contextName, PhaseContext parentPhaseContext, Guid cacheId, CourierFacade courier, ClusterMessenger messenger, StaticConfiguration staticConfiguration, LiveConfiguration liveConfiguration, SlaveBinaryLogContainer slaveBinaryLogContainer, Channel<IEntryOperationExecutionContext> inboundExecutionContextChannel, Partitioner partitioner) {
            this.contextName = contextName;
            this.parentPhaseContext = parentPhaseContext;
            CacheId = cacheId;
            Courier = courier;
            Messenger = messenger;
            Channels = new PhaseContextChannels(inboundExecutionContextChannel);
            StaticConfiguration = staticConfiguration;
            LiveConfiguration = liveConfiguration;
            SlaveBinaryLogContainer = slaveBinaryLogContainer;
            Partitioner = partitioner;
         }

         public async Task TransitionAsync(PhaseBase nextPhase) {
            var lastPhase = currentPhase;

            nextPhase.ThrowIfNull(nameof(nextPhase));
            nextPhase.Context = this;
            nextPhase.LastPhase = lastPhase;
            nextPhase.Generation = generationCounter++;

            Log($"Begin transition from ({currentPhase?.Generation ?? -1}) {currentPhase?.Description ?? "[null]"} to ({nextPhase.Generation}) {nextPhase.Description}.");

            if (lastPhase != null) {
               await lastPhase.HandleLeaveAsync();
               lastPhase.IsRunning = false;
            }

            currentPhase = nextPhase;
            nextPhase.IsRunning = true;
            
            await nextPhase.HandleEnterAsync().ConfigureAwait(false);
            if (nextPhase.IsRunning) {
               Go(async () => {
                  await nextPhase.RunAsync().ConfigureAwait(false);

                  if (nextPhase.IsRunning) {
                     Log($"Phase RunAsync completed without transition: ({nextPhase.Generation}) {nextPhase.Description}).");
                  }
               }).Forget();
            }
         }

         public Task ForkAsync(string name, PhaseBase nextPhase) {
            var newPhaseContext = new PhaseContext(name, this, CacheId, Courier, Messenger, StaticConfiguration, LiveConfiguration, SlaveBinaryLogContainer, Channels.InboundExecutionContextChannel, Partitioner);
            childContexts.AddOrThrow(newPhaseContext);
            Messenger.AddForkOrThrow(newPhaseContext);
            return newPhaseContext.TransitionAsync(nextPhase);
         }

         public void Log(string s) {
            lock (consoleLogLock) {
               Console.BackgroundColor = (ConsoleColor)((uint)Courier.Identity.Id.GetHashCode() % 7);
               Console.WriteLine($"{Courier.Identity.Id.ToString("n").Substring(0, 6)} [{contextName}]: " + s);
               Console.BackgroundColor = ConsoleColor.Black;
            }
         }

         public Guid CacheId { get; }
         public CourierFacade Courier { get; }
         public ClusterMessenger Messenger { get; }
         public PhaseContextChannels Channels { get; }
         public StaticConfiguration StaticConfiguration { get; }
         public LiveConfiguration LiveConfiguration { get; }
         public SlaveBinaryLogContainer SlaveBinaryLogContainer { get; }
         public Partitioner Partitioner { get; set; }

         public Task ProcessInboundMessageAsync<T>(IInboundMessageEvent<T> inboundMessageEvent) {
            return Task.WhenAll(
               Go(async () => {
                  if (typeof(T) == typeof(LeaderHeartBeatDto)) {
                     await Channels.LeaderHeartBeat.WriteAsync((IInboundMessageEvent<LeaderHeartBeatDto>)inboundMessageEvent);
                  } else if (typeof(T) == typeof(RepartitionCompleteDto)) {
                     await Channels.RepartitionComplete.WriteAsync((IInboundMessageEvent<RepartitionCompleteDto>)inboundMessageEvent);
                  } else if (typeof(T) == typeof(ElectDto)) {
                     await Channels.Elect.WriteAsync((IInboundMessageEvent<ElectDto>)inboundMessageEvent);
                  } else {
                     throw new NotSupportedException();
                  }
               }),
               Go(() => Task.WhenAll(childContexts.Select(childContext => childContext.ProcessInboundMessageAsync<T>(inboundMessageEvent)))));
         }
      }

      public class PhaseContextChannels {
         public PhaseContextChannels(Channel<IEntryOperationExecutionContext> inboundExecutionContextChannel) {
            InboundExecutionContextChannel = inboundExecutionContextChannel;
         }

         public Channel<IEntryOperationExecutionContext> InboundExecutionContextChannel { get; set; }
         public Channel<IInboundMessageEvent<LeaderHeartBeatDto>> LeaderHeartBeat { get; } = ChannelFactory.Blocking<IInboundMessageEvent<LeaderHeartBeatDto>>();
         public Channel<IInboundMessageEvent<RepartitionCompleteDto>> RepartitionComplete { get; } = ChannelFactory.Blocking<IInboundMessageEvent<RepartitionCompleteDto>>();
         public Channel<IInboundMessageEvent<ElectDto>> Elect { get; } = ChannelFactory.Blocking<IInboundMessageEvent<ElectDto>>();
      }

      public class IndeterminatePhase : PhaseBase {
         public override string Description => "[Indeterminate]";

         public override async Task RunAsync() {
            await new Select {
               Case(Time.After(5000), TransitionToElection),
               Case(Channels.Elect, TransitionToElection),
               Case(Channels.LeaderHeartBeat, FailNotImplemented)
            }.WaitAsync().ConfigureAwait(false);
         }

         private Task TransitionToElection() {
            IReadOnlySet<Guid> cohortIds = new HashSet<Guid> { Identity.Id };
            return TransitionAsync(new ElectionCandidatePhase(cohortIds));
         }
      }

      public class ElectionCandidatePhase : PhaseBase {
         private const int kDefaultTicksToVictory = 3;

         private readonly IReadOnlySet<Guid> cohortIds;
         private readonly int ticksToVictory;

         public ElectionCandidatePhase(IReadOnlySet<Guid> cohortIds) : this(cohortIds, kDefaultTicksToVictory) { }

         public ElectionCandidatePhase(IReadOnlySet<Guid> cohortIds, int ticksToVictory) {
            this.cohortIds = cohortIds;
            this.ticksToVictory = ticksToVictory;
         }

         public override string Description => $"[ElectionCandidate TTV={ticksToVictory}, Cohorts[{cohortIds.Count}]={cohortIds.Join(", ")})]";

         public override async Task HandleEnterAsync() {
            await base.HandleEnterAsync();

            await Messenger.SendToClusterAsync(new ElectDto {
               NomineeId = Identity.Id,
               FollowerIds = cohortIds
            });
         }

         public override async Task RunAsync() {
            var loop = true;
            while (IsRunning && loop) {
               loop = false;

               await new Select {
                  Case(Time.After(500), async () => {
                     if (ticksToVictory == 1) {
                        logger.Info("Party time!");
                        await TransitionAsync(new CoordinatorEntryPointPhase(cohortIds));
                     } else {
                        await TransitionAsync(new ElectionCandidatePhase(cohortIds, ticksToVictory - 1));
                     }
                  }),
                  Case(Channels.Elect, async message => {
                     var electDto = message.Body;
                     if (Identity.Id.CompareTo(electDto.NomineeId) < 0) {
                        await TransitionAsync(new ElectionFollowerPhase(electDto.NomineeId));
                     } else {
                        var nextCohortIds = new HashSet<Guid>(cohortIds);
                        if (nextCohortIds.Add(message.SenderId)) {
                           await TransitionAsync(new ElectionCandidatePhase(nextCohortIds, ticksToVictory + 1));
                        } else {
                           loop = true;
                        }
                     }
                  }),
                  Case(Channels.LeaderHeartBeat, FailNotImplemented)
               };
            }
         }
      }

      public static class SomeJankyCommonLogicThing {

      }

      public class ElectionFollowerPhase : PhaseBase {
         private readonly Guid leaderId;

         public ElectionFollowerPhase(Guid leaderId) {
            this.leaderId = leaderId;
         }

         public override string Description => $"[Follower LID={leaderId}]";

         public override async Task HandleEnterAsync() {
            await base.HandleEnterAsync();

            Messenger.SendToCohortReliableAsync(
               leaderId,
               new ElectDto {
                  NomineeId = leaderId,
                  FollowerIds = new HashSet<Guid>()
               }).Forget();
         }

         public override async Task RunAsync() {
            bool loop = true;
            int last = 0;
            while (IsRunning && loop) {
               loop = false;

               await new Select {
                  Case(Time.After(5000), async () => {
                     last = 1;
                     await TransitionAsync(new IndeterminatePhase());
                  }),
                  Case(Channels.Elect, async message => {
                     last = 2;
                     var electDto = message.Body;
                     if (leaderId.CompareTo(electDto.NomineeId) < 0) {
                        await TransitionAsync(new ElectionFollowerPhase(electDto.NomineeId));
                     } else {
                        loop = true;
                     }
                  }),
                  Case(Channels.LeaderHeartBeat, async x => {
                     last = 3;
                     if (x.Body.CohortIds.Contains(Identity.Id)) {
                        await TransitionAsync(new CohortRepartitionPhase(x.SenderId, x.Body.CohortIds));
                     } else {
                        await FailNotImplemented();
                     }
                  })
               };
            }

            if (IsRunning) {
               Console.WriteLine(last);
               throw new Exception("WTF");
            }
         }
      }

      public class CohortRepartitionPhase : PhaseBase {
         private readonly Guid leaderId;
         private readonly IReadOnlySet<Guid> cohortIds;

         public CohortRepartitionPhase(Guid leaderId, IReadOnlySet<Guid> cohortIds) {
            this.leaderId = leaderId;
            this.cohortIds = cohortIds;
         }

         public override string Description => $"[CohortRepartition Leader={leaderId}, Cohorts[{cohortIds.Count}]={cohortIds.Join(", ")}]";

         public override async Task RunAsync() {
            while (IsRunning) {
               await new Select {
                  Case(Channels.RepartitionComplete, async x => {
                     var binaryLog = new BinaryLog();
                     var rankedCohortIds = x.Body.RankedCohortIds.ToArray();
                     LiveConfiguration.LocalRank = Array.IndexOf(rankedCohortIds, Identity.Id);
                     LiveConfiguration.CohortIdsByRank = rankedCohortIds;
                     LiveConfiguration.PartitionsIdsByRank = x.Body.PartitionIdsByRank;

                     await TransitionAsync(new CohortMainLoopPhase(leaderId));
                  }),
                  Case(Channels.LeaderHeartBeat, () => {}),
                  Case(Time.After(5000), () => TransitionAsync(new IndeterminatePhase()))
               };
            }
         }
      }

      public class CohortMainLoopPhase : PhaseBase {
         private delegate Task VisitorFunc(CohortMainLoopPhase self, IEntryOperationExecutionContext entryOperation);
         private static readonly IGenericFlyweightFactory<VisitorFunc> processInboundExecutionContextVisitors
             = GenericFlyweightFactory.ForMethod<VisitorFunc>(
                typeof(CohortMainLoopPhase),
                nameof(ProcessInboundExecutionContextVisitor));

         private static Task ProcessInboundExecutionContextVisitor<TResult>(CohortMainLoopPhase self, IEntryOperationExecutionContext executionContext) {
            return self.ProcessInboundExecutionContextAsync((EntryOperationExecutionContext<TResult>)executionContext);
         }

         private readonly Guid leaderId;
         private readonly BinaryLog someBinaryLogThing = new BinaryLog();
         private readonly ConcurrentDictionary<K, SomethingToDoWithEntryOperationChuggingAbstractBeanFactorySingleton> somethingToDoWithEntryOperationChuggingAbstractBeanFactorySingletonByKey = new ConcurrentDictionary<K, SomethingToDoWithEntryOperationChuggingAbstractBeanFactorySingleton>();
         private Task replicationFromOthersTask;

         // HACK
         private int[] partitionIdsImInvolvedIn;
         private int partitionIOwn;
         private Guid[] slaveCohortIds;
         private SCG.IReadOnlyDictionary<Guid, SomeCohortContext> slaveCohortContextsById;
         private SCG.IReadOnlyDictionary<int, ICacheService<K, V>> cohortCacheServicesByRank;

         public CohortMainLoopPhase(Guid leaderId) {
            this.leaderId = leaderId;
         }

         public override string Description => $"[CohortMainLoop Leader={leaderId}, RankedCohorts[{LiveConfiguration.CohortIdsByRank.Length}]={LiveConfiguration.CohortIdsByRank.Join(", ")}]";

         public override Task HandleEnterAsync() {
            // we'll obviously clean this up later.
            partitionIdsImInvolvedIn = Enumerable.Range(0, StaticConfiguration.Redundancy)
                                               .Select(i => (i + LiveConfiguration.LocalRank) % LiveConfiguration.CohortCount)
                                               .ToArray();

            partitionIOwn = partitionIdsImInvolvedIn[0];

            slaveCohortIds = Enumerable.Range(1, StaticConfiguration.Redundancy - 1)
                                       .Select(i => (LiveConfiguration.LocalRank - i + LiveConfiguration.CohortCount) % LiveConfiguration.CohortCount)
                                       .Select(cohortRank => LiveConfiguration.CohortIdsByRank[cohortRank])
                                       .ToArray();

            slaveCohortContextsById = slaveCohortIds.ToDictionary(
               cohortId => cohortId,
               cohortId => new SomeCohortContext {
                  ReplicationState = new CohortReplicationState(),
                  ReplicationService = RemoteServiceProxyContainer.Get<ISomeReplicationService>(PeerTable.GetOrAdd(cohortId))
               });

            cohortCacheServicesByRank = Enumerable.Range(0, LiveConfiguration.CohortCount)
                                                  .ToDictionary(
                                                     cohortRank => cohortRank,
                                                     cohortRank => RemoteServiceProxyContainer.Get<ICacheService<K, V>>(
                                                        PeerTable.GetOrAdd(LiveConfiguration.CohortIdsByRank[cohortRank])
                                                        )
               );

            var partitionGuidsById = Enumerable.Range(0, LiveConfiguration.PartitionCount)
                                               .ToDictionary(
                                                  partitionId => partitionId,
                                                  partitionId => SomeHelperClass.AddToGuidSomehow(StaticConfiguration.CacheId, partitionId));

            var partitionIOwnGuid = partitionGuidsById[partitionIOwn];

            var slaveBinaryLogsByPartitionId = Enumerable.Range(0, LiveConfiguration.PartitionCount)
                                                    .ToDictionary(
                                                       partitionId => partitionId,
                                                       partitionId => new BinaryLog());

            foreach (var partitionIdImInvolvedIn in partitionIdsImInvolvedIn) {
               var partitionGuid = partitionGuidsById[partitionIdImInvolvedIn];
               var slaveBinaryLog = slaveBinaryLogsByPartitionId[partitionIdImInvolvedIn];
               Log("I am in partition guid " + partitionGuid);
               SlaveBinaryLogContainer.AddOrThrow(partitionGuid, slaveBinaryLog);
            }

            Log("I own partition guid " + partitionIOwnGuid);

            foreach (var cohortId in LiveConfiguration.CohortIdsByRank) {
               Log($"I know about cohort {cohortId}.");
               if (slaveCohortIds.Contains(cohortId)) {
                  Log($"Is my slave");
               }
               if (Identity.Id == cohortId) {
                  Log($"Is me");
               }
            }

            // Leader logic
            Go(async () => {
               await Task.Delay(5000);

               while (true) {
                  try {
                     Log("Entered main loop iteration");
                     // sync log entries
                     foreach (var kvp in slaveCohortContextsById) {
                        var cohortId = kvp.Key;
                        var cohortContext = kvp.Value;

                        var nextEntryIdToSync = cohortContext.ReplicationState.NextEntryIdToSync;
                        var entriesThatNeedToBSynced = await someBinaryLogThing.GetEntriesFrom(nextEntryIdToSync).ConfigureAwait(false);

                        if (entriesThatNeedToBSynced.Any()) {
                           try {
                              await cohortContext.ReplicationService.SyncAsync(partitionIOwnGuid, entriesThatNeedToBSynced).ConfigureAwait(false);
                              await cohortContext.ReplicationState.UpdateNextEntryIdToSync(entriesThatNeedToBSynced.Last().Id + 1).ConfigureAwait(false);
                              Log($"Got cohort {cohortId.ToShortString()} synced to {entriesThatNeedToBSynced.Last().Id}.");
                           } catch (RemoteException e) {
                              logger.Error("Something bad happened at sync.", e);
                           }
                        }
                     }

                     // advance commit pointer
                     var greatestFullySyncedEntryId = slaveCohortContextsById.Values.Min(x => x.ReplicationState.NextEntryIdToSync) - 1;
                     if (greatestFullySyncedEntryId > await someBinaryLogThing.GetGreatestCommittedEntryId().ConfigureAwait(false)) {
                        await someBinaryLogThing.UpdateGreatestCommittedEntryId(greatestFullySyncedEntryId);
                     }

                     // sync commit pointer
                     foreach (var kvp in slaveCohortContextsById) {
                        var cohortId = kvp.Key;
                        var cohortContext = kvp.Value;

                        var greatestCommittedEntryId = await someBinaryLogThing.GetGreatestCommittedEntryId().ConfigureAwait(false);

                        if (cohortContext.ReplicationState.GreatestCommittedEntryId < greatestCommittedEntryId) {
                           try {
                              await cohortContext.ReplicationService.CommitAsync(partitionIOwnGuid, greatestCommittedEntryId).ConfigureAwait(false);
                              await cohortContext.ReplicationState.UpdateGreatestCommittedEntryId(greatestCommittedEntryId).ConfigureAwait(false);
                              Log($"Got cohort {cohortId.ToShortString()} commit to {greatestCommittedEntryId}.");
                              Console.Title = ($"Got cohort {cohortId.ToShortString()} commit to {greatestCommittedEntryId}.");
                           } catch (RemoteException e) {
                              logger.Error("Something bad happened at commit.", e);
                           }
                        }
                     }

                     await Task.Delay(1000).ConfigureAwait(false);
                  } catch (Exception e) {
                     logger.Error("We threw a ", e);
                  }
               }
            }).Forget();

            return base.HandleEnterAsync();
         }

         public override async Task RunAsync() {
            while (IsRunning) {
               await new Select {
                  Case(Channels.LeaderHeartBeat, () => {}),
                  Case(Time.After(5000), () => TransitionAsync(new IndeterminatePhase())),
                  Case(Channels.InboundExecutionContextChannel, x => {
                     var tResult = x.GetType().GetGenericArguments()[0];
                     processInboundExecutionContextVisitors.Get(tResult)(this, x);
                  })
               };
            }
         }

         public async Task ProcessInboundExecutionContextAsync<TResult>(EntryOperationExecutionContext<TResult> executionContext) {
            var isReadOperation = executionContext.Operation.Type == EntryOperationType.Read;

            var partitionId = Partitioner.ComputePartitionId(executionContext.Operation.Key);
            if (!partitionIdsImInvolvedIn.Contains(partitionId)) {
               await HandleEntryOperationExecutionProxy(executionContext, isReadOperation);
            } else if (partitionIOwn != partitionId && !isReadOperation) {
               await HandleEntryOperationExecutionProxy(executionContext, false);
               return;
            } else if (isReadOperation) {
               var somethingToDoWithEntryOperationChuggingAbstractBeanFactorySingleton = somethingToDoWithEntryOperationChuggingAbstractBeanFactorySingletonByKey.GetOrAdd(
                  executionContext.Operation.Key,
                  add => new SomethingToDoWithEntryOperationChuggingAbstractBeanFactorySingleton());

               var result = await somethingToDoWithEntryOperationChuggingAbstractBeanFactorySingleton.EnqueueOperationAndGetResultAsync(executionContext.Operation);
               executionContext.ResultBox.SetResult(result);
            } else {
               someBinaryLogThing.AppendAsync()
            }
         }

         private async Task HandleEntryOperationExecutionProxy<TResult>(EntryOperationExecutionContext<TResult> executionContext, bool includeProxyToSlaves) {
            var partitionId = Partitioner.ComputePartitionId(executionContext.Operation.Key);
            var cohortRankOffset = includeProxyToSlaves ? StaticRandom.Next(StaticConfiguration.Redundancy) : 0;
            int peerRankToDispatchTo = (partitionId + cohortRankOffset) % LiveConfiguration.CohortCount;
            var peerCacheService = cohortCacheServicesByRank[peerRankToDispatchTo];
            var result = await peerCacheService.ProcessEntryOperationAsync(CacheId, executionContext.Operation);
            executionContext.ResultBox.SetResult(result);
         }

         public class SomeCohortContext {
            public CohortReplicationState ReplicationState { get; set; }
            public ISomeReplicationService ReplicationService { get; set; }
         }
      }

      public class CoordinatorEntryPointPhase : PhaseBase {
         private readonly IReadOnlySet<Guid> cohortIds;
         private Task heartBeatTask;

         public CoordinatorEntryPointPhase(IReadOnlySet<Guid> cohortIds) {
            this.cohortIds = cohortIds;
         }

         public override string Description => $"[CoordinatorEntryPoint Cohorts[{cohortIds.Count}]={cohortIds.Join(", ")}]";

         public override async Task HandleEnterAsync() {
            await base.HandleEnterAsync();

            await Context.ForkAsync("FORK", new CohortRepartitionPhase(Identity.Id, cohortIds));

            heartBeatTask = Go(async () => {
               while (true) {
                  await Messenger.SendToClusterAsync(new LeaderHeartBeatDto {
                     CohortIds = cohortIds
                  }).ConfigureAwait(false);
                  await Task.Delay(500).ConfigureAwait(false);
               }
            });

            // repartition logic
            var rankedCohorts = new SortedSet<Guid>(cohortIds).ToArray();
            foreach (var cohort in rankedCohorts) {
               var cohortPeerContext = PeerTable.GetOrAdd(cohort);
               var haves = RemoteServiceProxyContainer.Get<ISomeRepartitioningService>(cohortPeerContext);
            }

            // divvy up responsibility for data partitions
            LiveConfiguration.LocalRank = Array.IndexOf(rankedCohorts, Identity.Id);
            LiveConfiguration.CohortIdsByRank = rankedCohorts;
            var partitionIdsByRank = new SCG.Dictionary<int, IReadOnlySet<int>>();
            foreach (var cohortRank in Enumerable.Range(0, rankedCohorts.Length)) {
               partitionIdsByRank.Add(
                  cohortRank,
                  new HashSet<int>(
                     Enumerable.Range(0, StaticConfiguration.Redundancy)
                               .Select(j => (cohortRank + j) % rankedCohorts.Length)
                     ));
            }
            LiveConfiguration.PartitionsIdsByRank = partitionIdsByRank;

            await Messenger.SendToClusterReliableAsync(cohortIds, new RepartitionCompleteDto(rankedCohorts, partitionIdsByRank));
            await TransitionAsync(new CoordinatorMainLoopPhase(cohortIds));
         }

         public override Task RunAsync() => FailInvalidState();
      }

      public class CoordinatorMainLoopPhase : PhaseBase {
         private readonly IReadOnlySet<Guid> cohortIds;

         public CoordinatorMainLoopPhase(IReadOnlySet<Guid> cohortIds) {
            this.cohortIds = cohortIds;
         }

         public override string Description => $"[CoordinatorMainLoop Cohorts[{cohortIds.Count}]={cohortIds.Join(", ")}]";

         public override async Task RunAsync() {
            while (true) {
               await Task.Yield();
            }
         }
      }
      
      public class ClusterMessenger {
         private readonly ConcurrentSet<PhaseContext> forkPhaseContexts = new ConcurrentSet<PhaseContext>();
         private readonly CourierFacade courier;

         public ClusterMessenger(CourierFacade courier) {
            this.courier = courier;
         }

         public Task SendToClusterAsync<T>(T payload) {
            SendToForks(Guid.Empty, payload);
            return courier.BroadcastAsync(payload);
         }

         public Task SendToClusterReliableAsync<T>(IReadOnlySet<Guid> cohortIds, T payload) {
            SendToForks(null, payload);
            return Task.WhenAll(cohortIds.Select(cohortId => SendToCohortReliableAsync(cohortId, payload)));
         }

         public Task SendToCohortReliableAsync<T>(Guid cohortId, T payload) {
            SendToForks(cohortId, payload);
            return courier.SendReliableAsync(payload, cohortId);
         }

         public Task SendToCohortUnreliableAsync<T>(Guid cohortId, T payload) {
            SendToForks(cohortId, payload);
            return courier.SendUnreliableAsync(payload, cohortId);
         }

         public void AddForkOrThrow(PhaseContext newPhaseContext) {
            forkPhaseContexts.AddOrThrow(newPhaseContext);
         }
         
         // HACK - needs a lot of cleanup =(
         private void SendToForks<T>(Guid? destinationCohortIdOrNullForTheForkCohortId, T payload) {
            var inboundMessageEvent = new InboundMessageEvent<T> {
               Message = new MessageDto {
                  Body = payload,
                  ReceiverId = Guid.Empty,
                  SenderId = courier.Identity.Id
               }
            };
            foreach (var forkPhaseContext in forkPhaseContexts) {
               if (destinationCohortIdOrNullForTheForkCohortId == null || forkPhaseContext.Courier.Identity.Matches(destinationCohortIdOrNullForTheForkCohortId.Value, IdentityMatchingScope.Broadcast)) {
                  forkPhaseContext.ProcessInboundMessageAsync(inboundMessageEvent).Forget();
               }
            }
         }
      }

      [Guid("811366D4-8959-4EE6-8FB7-2BDDF3C44C21")]
      public interface ISomeRepartitioningService {
         SCG.IReadOnlyList<HaveDto> GetHaves();
      }

      public class SomeRepartitioningService : ISomeRepartitioningService {
         public SCG.IReadOnlyList<HaveDto> GetHaves() {
            return new SCG.List<HaveDto>();
         }
      }

      [Guid("61701EB0-3648-4505-9CEB-C9A78AF906A4")]
      public interface ISomeReplicationService {
         Task SyncAsync(Guid binaryLogId, SCG.IReadOnlyList<BinaryLogEntry> newLogEntries);
         Task CommitAsync(Guid binaryLogId, int entryId);
      }

      public class SomeReplicationService : ISomeReplicationService {
         private readonly SlaveBinaryLogContainer slaveBinaryLogContainer;

         public SomeReplicationService(SlaveBinaryLogContainer slaveBinaryLogContainer) {
            this.slaveBinaryLogContainer = slaveBinaryLogContainer;
         }

         public Task SyncAsync(Guid binaryLogId, SCG.IReadOnlyList<BinaryLogEntry> newLogEntries) {
            var binaryLog = slaveBinaryLogContainer.GetOrThrow(binaryLogId);
            return binaryLog.SomethingToDoWithSyncing(newLogEntries);
         }

         public Task CommitAsync(Guid binaryLogId, int entryId) {
            var binaryLog = slaveBinaryLogContainer.GetOrThrow(binaryLogId);
            return binaryLog.UpdateGreatestCommittedEntryId(entryId);
         }
      }

      [Guid("0B94DAF3-7FD6-4414-A668-43FA0DDB0B48")]
      public interface ICacheService<K, V> {
         Task<TResult> ProcessEntryOperationAsync<TResult>(Guid cacheId, IEntryOperation<TResult> entryOperation);
      }

      public class CacheService<K, V> : ICacheService<K, V> {
         private readonly Something something;

         public CacheService(Something something) {
            this.something = something;
         }

         public async Task<TResult> ProcessEntryOperationAsync<TResult>(Guid cacheId, IEntryOperation<TResult> entryOperation) {
            var executionContext = new EntryOperationExecutionContext<TResult> {
               Operation = entryOperation
            };
            await something.SomethingToDoWithAddingAExecutionContextToAChannel(cacheId, executionContext).ConfigureAwait(false);
            return await executionContext.ResultBox.GetResultAsync().ConfigureAwait(false);
         }
      }

      //         private async Task ProcessEntryUpdateDtoAsync(EntryUpdateDto message) {
      //            throw new NotImplementedException();
      //         }

      public class Partitioner {
         private const int kHashBitCount = 32;
         private readonly StaticConfiguration staticConfiguration;
         private readonly LiveConfiguration liveConfiguration;

         public Partitioner(StaticConfiguration staticConfiguration, LiveConfiguration liveConfiguration) {
            this.staticConfiguration = staticConfiguration;
            this.liveConfiguration = liveConfiguration;
         }

         public int ComputeBlockId(K key) {
            var hash = (uint)key.GetHashCode();
            return (int)(hash >> (kHashBitCount - staticConfiguration.BlockCountPower));
         }

         public int ComputePartitionId(K key) => ComputePartitionId(ComputeBlockId(key));

         public int ComputePartitionId(int blockId) {
            return ComputePartitionIdWithBlockCount(blockId, staticConfiguration.DerivedBlockCount);
         }

         public int ComputePartitionIdWithBlockCount(int blockId, int blockCount) {
            int blocksPerPartition = blockCount / liveConfiguration.PartitionCount;
            return blockId / blocksPerPartition;
         }

         public bool IsLocallyMasteredKey(K key) => IsLocallyMasteredPartition(ComputePartitionId(key));

         public bool IsLocallyMasteredPartition(int partition) {
            var partitionIdDifference = partition - liveConfiguration.LocalRank;
            if (partitionIdDifference < 0) {
               partitionIdDifference += liveConfiguration.PartitionCount;
            }
            return partitionIdDifference < staticConfiguration.Redundancy;
         }
      }

      public class Synchronizer {
         private readonly AsyncReaderWriterLock inner = new AsyncReaderWriterLock();

         public Task<IDisposable> Big(CancellationToken token = default(CancellationToken)) => inner.WriterLockAsync(token);
         public Task<IDisposable> Small(CancellationToken token = default(CancellationToken)) => inner.ReaderLockAsync(token);
      }
   }
}
