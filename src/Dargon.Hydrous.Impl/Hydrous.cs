using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Channels;
using Dargon.Commons.Collections;
using Dargon.Commons.Exceptions;
using Dargon.Courier;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.ServiceTier;
using Dargon.Courier.ServiceTier.Client;
using Dargon.Courier.Vox;
using Dargon.Hydrous.Impl.BinaryLogNamespaceThing;
using Dargon.Hydrous.Impl.Diagnostics;
using Dargon.Hydrous.Impl.Store;
using Dargon.Vox;
using Dargon.Vox.Utilities;
using NLog;
using System;
using System.CodeDom;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static Dargon.Commons.Channels.ChannelsExtensions;
using SCG = System.Collections.Generic;

namespace Dargon.Hydrous.Impl {
   public interface ICacheService<K, V> {
      Task<TResult> ProcessEntryOperationAsync<TResult>(K key, IEntryOperation<K, V, TResult> entryOperation);
   }

   public class UserCacheImpl<K, V> : ICache<K, V> {
      private readonly ICacheService<K, V> cacheService;

      public UserCacheImpl(ICacheService<K, V> cacheService) {
         this.cacheService = cacheService;
      }

      public async Task<IReadableEntry<K, V>> GetAsync(K key) {
         var operation = ReadEntryOperation<K, V>.Create();
         return await cacheService.ProcessEntryOperationAsync(key, operation).ConfigureAwait(false);
      }

      public async Task<IReadableEntry<K, V>> PutAsync(K key, V value) {
         var operation = PutEntryOperation<K, V>.Create(value);
         return await cacheService.ProcessEntryOperationAsync(key, operation).ConfigureAwait(false);
      }

      public Task<R> ProcessAsync<R>(K key, IEntryOperation<K, V, R> operation) {
         return cacheService.ProcessEntryOperationAsync(key, operation);
      }

      public async Task<SCG.IReadOnlyDictionary<K, R>> ProcessManyAsync<R>(IReadOnlySet<K> keys, IEntryOperation<K, V, R> operation) {
         var tasksByKey = keys.ToDictionary(
            key => key,
            key => ProcessAsync(key, operation));

         var results = new SCG.Dictionary<K, R>();
         foreach (var kvp in tasksByKey) {
            results.Add(kvp.Key, await kvp.Value.ConfigureAwait(false));
         }
         return results;
      }
   }

   public class CacheService<K, V> : ICacheService<K, V> {
      private readonly Channel<CacheRoot<K, V>.IEntryOperationExecutionContext> inboundExecutionContextChannel;
      private readonly OperationDiagnosticsTable operationDiagnosticsTable;

      public CacheService(Channel<CacheRoot<K, V>.IEntryOperationExecutionContext> inboundExecutionContextChannel, OperationDiagnosticsTable operationDiagnosticsTable) {
         this.inboundExecutionContextChannel = inboundExecutionContextChannel;
         this.operationDiagnosticsTable = operationDiagnosticsTable;
      }

      public async Task<TResult> ProcessEntryOperationAsync<TResult>(K key, IEntryOperation<K, V, TResult> entryOperation) {
         operationDiagnosticsTable.Create(
            entryOperation.Id, 
            entryOperation.GetType().Name + " " + key,
            $"Via {nameof(ProcessEntryOperationAsync)}");

         var executionContext = CacheRoot<K, V>.EntryOperationExecutionContext<TResult>.Create(key, entryOperation, operationDiagnosticsTable);

         operationDiagnosticsTable.UpdateStatus(entryOperation.Id, 1, "Adding to IEC");
         await inboundExecutionContextChannel.WriteAsync(executionContext).ConfigureAwait(false);
         operationDiagnosticsTable.UpdateStatus(entryOperation.Id, 1, "Added to IEC");
         var result = await executionContext.ResultBox.GetResultAsync().ConfigureAwait(false);
         operationDiagnosticsTable.Destroy(entryOperation.Id);
         return result;
      }
   }

   public class CacheDebugMob<K, V> {
      private readonly CacheConfiguration<K, V> cacheConfiguration;
      private readonly CacheRoot<K, V>.LiveClusterConfiguration liveClusterConfiguration;
      private readonly ICacheService<K, V> cacheService;
      private readonly CacheRoot<K, V>.Partitioner partitioner;
      private readonly OperationDiagnosticsTable operationDiagnosticsTable;

      public CacheDebugMob(CacheConfiguration<K, V> cacheConfiguration, CacheRoot<K, V>.LiveClusterConfiguration liveClusterConfiguration, ICacheService<K, V> cacheService, CacheRoot<K, V>.Partitioner partitioner, OperationDiagnosticsTable operationDiagnosticsTable) {
         this.cacheConfiguration = cacheConfiguration;
         this.liveClusterConfiguration = liveClusterConfiguration;
         this.cacheService = cacheService;
         this.partitioner = partitioner;
         this.operationDiagnosticsTable = operationDiagnosticsTable;
      }

      [ManagedProperty]
      public CacheConfiguration<K, V> CacheConfiguration => cacheConfiguration;

      [ManagedProperty]
      public CacheRoot<K, V>.LiveClusterConfiguration LiveClusterConfiguration => liveClusterConfiguration;

      [ManagedProperty]
      public SCG.IEnumerable<OperationDiagnosticStateDto> Operations => operationDiagnosticsTable.Enumerate();

      [ManagedOperation]
      public async Task<Entry<K, V>> Get(K key) {
         return await cacheService.ProcessEntryOperationAsync(key, ReadEntryOperation<K, V>.Create()).ConfigureAwait(false);
      }

      [ManagedOperation]
      public async Task<Entry<K, V>> Put(K key, V value) {
         return await cacheService.ProcessEntryOperationAsync(key, PutEntryOperation<K, V>.Create(value)).ConfigureAwait(false);
      }

      [ManagedOperation]
      public int GetPartitionIndex(K key) => partitioner.ComputePartitionIndex(key);

      [ManagedOperation]
      public int GetBlockId(K key) => partitioner.ComputeBlockId(key);
   }

   public class HydarVoxTypes : VoxTypes {
      public HydarVoxTypes() : base(100) {
         // General Stuff
         Register(0, typeof(CacheConfiguration<,>));

         // Cache Operations
         Register(10, typeof(ReadEntryOperation<,>));
         Register(11, typeof(PutEntryOperation<,>));

         // Cache Clustering Configuration
         Register(20, typeof(StaticClusterConfiguration));
         Register(21, typeof(CacheRoot<,>.LiveClusterConfiguration));
         Register(22, typeof(PartitioningConfiguration));

         // Cache Clustering DTOs
         Register(30, typeof(ElectDto));
         Register(31, typeof(LeaderHeartBeatDto));
         Register(32, typeof(RepartitionCompleteDto));
         Register(33, typeof(BinaryLogEntry));
         Register(34, typeof(Entry<,>));

         // Cache Clustering - Replication / Binary Log
         Register(40, typeof(CacheRoot<,>.CohortContext));
         Register(41, typeof(CohortReplicationState));
         Register(42, typeof(CacheRoot<,>.EntryOperationBinaryLogData<>));
         Register(43, typeof(CommitOperationProcessedDto));
         
         // Cache Diagnostics
         Register(50, typeof(OperationDiagnosticStateDto));
      }
   }

   [AutoSerializable]
   public class ElectDto {
      public Guid SenderId { get; set; }
      public Guid NomineeId { get; set; }
      public IReadOnlySet<Guid> FollowerIds { get; set; }

      public override string ToString() => "Nominee: " + NomineeId + " Followers: " + FollowerIds.Join(", ");
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
//      public IEntryOperation<K, V> Operation { get; set; }
   }

   [AutoSerializable]
   public class HaveDto {
      public int EpochId { get; set; }
      public int BlockId { get; set; }
   }

   [AutoSerializable]
   public class EntryUpdateDto { }
   
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

   [AutoSerializable]
   public class CommitOperationProcessedDto {
      public Guid OperationId { get; set; }
   }

   public class CacheRoot<K, V> : ICacheFacade<K, V> {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      public CacheRoot(ICacheService<K, V> cacheService, ICache<K, V> userCache) {
         CacheService = cacheService;
         UserCache = userCache;
      }

      public ICacheService<K, V> CacheService { get; }
      public ICache<K, V> UserCache { get; }

      public static CacheRoot<K, V> Create(CourierFacade courier, CacheConfiguration<K, V> cacheConfiguration) {
         var identity = courier.Identity;
         var router = courier.InboundMessageRouter;
         var liveConfiguration = new LiveClusterConfiguration();
         var partitioner = new Partitioner(cacheConfiguration.PartitioningConfiguration, liveConfiguration);

         var clusterMessenger = new ClusterMessenger(courier);

         var slaveBinaryLogContainer = new SlaveBinaryLogContainer();
         var inboundExecutionContextChannel = ChannelFactory.Nonblocking<IEntryOperationExecutionContext>();

         var operationDiagnosticsTable = new OperationDiagnosticsTable(identity);

         var phaseContext = new PhaseContext("MAIN", identity, null, cacheConfiguration.CacheId, courier, clusterMessenger, cacheConfiguration, liveConfiguration, slaveBinaryLogContainer, inboundExecutionContextChannel, partitioner, operationDiagnosticsTable);
         phaseContext.TransitionAsync(new IndeterminatePhase()).Forget();

         router.RegisterHandler<ElectDto>(SomeCloningProxy<ElectDto>(phaseContext));
         router.RegisterHandler<LeaderHeartBeatDto>(SomeCloningProxy<LeaderHeartBeatDto>(phaseContext));
         router.RegisterHandler<RepartitionHaltDto>(SomeCloningProxy<RepartitionHaltDto>(phaseContext));
         router.RegisterHandler<RepartitionCompleteDto>(SomeCloningProxy<RepartitionCompleteDto>(phaseContext));
         router.RegisterHandler<RequestDto<K, V>>(SomeCloningProxy<RequestDto<K, V>>(phaseContext));
         router.RegisterHandler<EntryUpdateDto>(SomeCloningProxy<EntryUpdateDto>(phaseContext));
         router.RegisterHandler<CommitOperationProcessedDto>(SomeCloningProxy<CommitOperationProcessedDto>(phaseContext));

         var someRepartitioningService = new SomeRepartitioningService();
         courier.LocalServiceRegistry.RegisterService<ISomeRepartitioningService>(someRepartitioningService);

         var someReplicationService = new SomeReplicationService(slaveBinaryLogContainer);
         courier.LocalServiceRegistry.RegisterService<ISomeReplicationService>(someReplicationService);

         var cacheService = new CacheService<K, V>(inboundExecutionContextChannel, operationDiagnosticsTable);
         courier.LocalServiceRegistry.RegisterService<ICacheService<K, V>>(cacheConfiguration.CacheId, cacheService);
         courier.MobOperations.RegisterMob(cacheConfiguration.CacheId, new CacheDebugMob<K, V>(cacheConfiguration, liveConfiguration, cacheService, partitioner, operationDiagnosticsTable), $"@Hydar/{cacheConfiguration.CacheName}");

         var userCache = new UserCacheImpl<K, V>(cacheService);

         return new CacheRoot<K, V>(cacheService, userCache);
      }

      public static Func<IInboundMessageEvent<T>, Task> SomeCloningProxy<T>(PhaseContext phaseContext) {
         return async x => {
            await TaskEx.YieldToThreadPool();

            var clone = new InboundMessageEvent<T>();
            clone.Message = x.Message;
            clone.Sender = x.Sender;

            logger.Info("Processing IME " + clone);
            phaseContext.ProcessInboundMessageAsync(clone).Forget();
         };
      }

      [AutoSerializable]
      public class LiveClusterConfiguration {
         public int LocalRank { get; set; }
         public Guid[] CohortIdsByRank { get; set; }
         public int CohortCount => CohortIdsByRank.Length;
         public int PartitionCount => CohortCount;
         public SCG.IReadOnlyList<Guid> PartitionGuidsByIndex { get; set; } 
         public SCG.IReadOnlyDictionary<int, IReadOnlySet<int>> AssignedPartitionsIndicesByRank { get; set; }
         public SCG.IReadOnlyList<int> AssignedPartitionIndices { get; set; }
         public int LedPartitionIndex => AssignedPartitionIndices[0];
         public Guid LedPartitionId => PartitionGuidsByIndex[LedPartitionIndex];
         public SCG.IReadOnlyList<int> ReplicaCohortRanks { get; set; } 
         public SCG.IReadOnlyList<Guid> ReplicaCohortIds { get; set; } 
         public SCG.IReadOnlyDictionary<Guid, CohortContext> CohortContextsById { get; set; }
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

      public interface IEntryOperationExecutionContext {
         IEntryOperation Operation { get; }
         Task ExecuteAsync(Entry<K, V> entry);
      }

      public class EntryOperationExecutionContext<TResult> : IEntryOperationExecutionContext {
         public AsyncBox<TResult> ResultBox { get; } = new AsyncBox<TResult>();
         IEntryOperation IEntryOperationExecutionContext.Operation => Operation;
         public K Key { get; set; }
         public IEntryOperation<K, V, TResult> Operation { get; set; }
         public OperationDiagnosticsTable OperationDiagnosticsTable { get; set; }

         private EntryOperationExecutionContext() { }

         public async Task ExecuteAsync(Entry<K, V> entry) {
            OperationDiagnosticsTable.UpdateStatus(Operation.Id, 8, "Entering execute async");
            var result = await Operation.ExecuteAsync(entry).ConfigureAwait(false);
            OperationDiagnosticsTable.UpdateStatus(Operation.Id, 8, "Leaving execute async (*)");
            ResultBox.SetResult(result);
         }

         public static EntryOperationExecutionContext<TResult> Create(K key, IEntryOperation<K, V, TResult> operation, OperationDiagnosticsTable operationDiagnosticsTable) {
            operation.ThrowIfNull(nameof(operation));
            return new EntryOperationExecutionContext<TResult> {
               Key = key,
               Operation = operation,
               OperationDiagnosticsTable = operationDiagnosticsTable
            };
         }
      }

      public class SomethingToDoWithEntryOperationChuggingAbstractBeanFactorySingleton {
         private readonly AsyncSemaphore operationsAvailableSignal = new AsyncSemaphore();
         private readonly ConcurrentQueue<IEntryOperationExecutionContext> readOperationExecutionContextQueue = new ConcurrentQueue<IEntryOperationExecutionContext>();
         private readonly ConcurrentQueue<IEntryOperationExecutionContext> putOperationExecutionContextQueue = new ConcurrentQueue<IEntryOperationExecutionContext>();
         private readonly ConcurrentQueue<IEntryOperationExecutionContext> conditionalUpdateOperationExecutionContextQueue = new ConcurrentQueue<IEntryOperationExecutionContext>();
         private readonly ConcurrentQueue<IEntryOperationExecutionContext> nonconditionalUpdateOperationExecutionContextQueue = new ConcurrentQueue<IEntryOperationExecutionContext>();
         private readonly K key;
         private readonly ICachePersistenceStrategy<K, V> cachePersistenceStrategy;
         private readonly OperationDiagnosticsTable operationDiagnosticsTable;

         public SomethingToDoWithEntryOperationChuggingAbstractBeanFactorySingleton(K key, ICachePersistenceStrategy<K, V> cachePersistenceStrategy, OperationDiagnosticsTable operationDiagnosticsTable) {
            this.key = key;
            this.cachePersistenceStrategy = cachePersistenceStrategy;
            this.operationDiagnosticsTable = operationDiagnosticsTable;
         }

         public async Task InitializeAsync() {
            try {
               var baseEntry = await cachePersistenceStrategy.ReadAsync(key).ConfigureAwait(false);
               RunAsync(baseEntry).Forget();
            } catch (Exception e) {
               logger.Fatal("Initialize async for entry of key " + key + " threw:" , e);
               throw;
            }
         }

         public async Task RunAsync(Entry<K, V> baseEntry) {
            while (true) {
               try {
                  // take a count which indicates work is available, then return it
                  await operationsAvailableSignal.WaitAsync().ConfigureAwait(false);
                  operationsAvailableSignal.Release();

                  // Process reads
                  ProcessReadsAsync(baseEntry).Forget();

                  // Process writes/modified
                  var updatedEntry = baseEntry.DeepCloneSerializable();
                  var entryModified = false;
                  entryModified |= await ProcessPutsAsync(updatedEntry).ConfigureAwait(false);
                  entryModified |= await ProcessAllUpdatesAsync(updatedEntry).ConfigureAwait(false);

                  // If modified, then persist to backing store
                  if (entryModified) {
                     await cachePersistenceStrategy.HandleUpdateAsync(baseEntry, updatedEntry).ConfigureAwait(false);
                  }
                  baseEntry = updatedEntry;
               } catch (Exception e) {
                  logger.Fatal("Aborting as RunAsync on entry " + baseEntry + " threw", e);
                  throw;
               }
            }
         }

         private async Task ProcessReadsAsync(Entry<K, V> entry) {
            IEntryOperationExecutionContext executionContext;
            var readExecutionContexts = new SCG.List<IEntryOperationExecutionContext>();
            while (readOperationExecutionContextQueue.TryDequeue(out executionContext)) {
               operationDiagnosticsTable.UpdateStatus(executionContext.Operation.Id, 7, "READ Taking Signal");
               await operationsAvailableSignal.WaitAsync().ConfigureAwait(false);
               operationDiagnosticsTable.UpdateStatus(executionContext.Operation.Id, 7, "READ Took Signal");

               readExecutionContexts.Add(executionContext);
            }

            logger.Info($"Processing {readExecutionContexts.Count} reads on entry {entry}.");
            await Task.WhenAll(
               readExecutionContexts.Map(async ec => {
                  operationDiagnosticsTable.UpdateStatus(ec.Operation.Id, 7, "READ Executing (*)");
                  await ec.ExecuteAsync(entry).ConfigureAwait(false);
                  return ec.ExecuteAsync(entry);
               })).ConfigureAwait(false);
            logger.Info($"Done processing {readExecutionContexts.Count} reads on entry {entry}.");
         }

         private async Task<bool> ProcessPutsAsync(Entry<K, V> entry) {
            bool entryModified = false;

            IEntryOperationExecutionContext executionContext;
            while (putOperationExecutionContextQueue.TryDequeue(out executionContext)) {
               operationDiagnosticsTable.UpdateStatus(executionContext.Operation.Id, 7, "PUT Taking Signal");
               await operationsAvailableSignal.WaitAsync().ConfigureAwait(false);
               operationDiagnosticsTable.UpdateStatus(executionContext.Operation.Id, 7, "PUT Took Signal (*)");

               logger.Info($"Processing put on entry {entry}.");
               await executionContext.ExecuteAsync(entry).ConfigureAwait(false);

               entryModified = true;
               entry.IsDirty = false;
            }

            return entryModified;
         }

         private async Task<bool> ProcessAllUpdatesAsync(Entry<K, V> entry) {
            bool entryModified = false;

            IEntryOperationExecutionContext executionContext;
            while (conditionalUpdateOperationExecutionContextQueue.TryDequeue(out executionContext)) {
               operationDiagnosticsTable.UpdateStatus(executionContext.Operation.Id, 7, "CU Taking Signal");
               await operationsAvailableSignal.WaitAsync().ConfigureAwait(false);
               operationDiagnosticsTable.UpdateStatus(executionContext.Operation.Id, 7, "CU Took Signal (*)");

               logger.Info($"Processing conditional update on entry {entry}.");
               await executionContext.ExecuteAsync(entry).ConfigureAwait(false);

               if (entry.IsDirty) {
                  entryModified = true;
                  entry.IsDirty = false;
               }
            }

            while (nonconditionalUpdateOperationExecutionContextQueue.TryDequeue(out executionContext)) {
               operationDiagnosticsTable.UpdateStatus(executionContext.Operation.Id, 7, "NCU Taking Signal");
               await operationsAvailableSignal.WaitAsync().ConfigureAwait(false);
               operationDiagnosticsTable.UpdateStatus(executionContext.Operation.Id, 7, "NCU Took Signal (*)");

               logger.Info($"Processing nonconditional update on entry {entry}.");
               await executionContext.ExecuteAsync(entry).ConfigureAwait(false);
               entryModified = true;
            }

            return entryModified;
         }

         public EntryOperationExecutionContext<TResult> EnqueueOperationAndGetExecutionContext<TResult>(K key, IEntryOperation<K, V, TResult> entryOperation) {
            operationDiagnosticsTable.UpdateStatus(entryOperation.Id, 6, "Enter Op Enqueue");
            var executionContext = EntryOperationExecutionContext<TResult>.Create(key, entryOperation, operationDiagnosticsTable);
            switch (executionContext.Operation.Type) {
               case EntryOperationType.Read:
                  operationDiagnosticsTable.UpdateStatus(entryOperation.Id, 6, "Enqueuing to Read");
                  readOperationExecutionContextQueue.Enqueue(executionContext);
                  break;
               case EntryOperationType.Put:
                  operationDiagnosticsTable.UpdateStatus(entryOperation.Id, 6, "Enqueuing to Put");
                  putOperationExecutionContextQueue.Enqueue(executionContext);
                  break;
               case EntryOperationType.ConditionalUpdate:
                  operationDiagnosticsTable.UpdateStatus(entryOperation.Id, 6, "Enqueuing to Conditional Update");
                  conditionalUpdateOperationExecutionContextQueue.Enqueue(executionContext);
                  break;
               case EntryOperationType.Update:
                  operationDiagnosticsTable.UpdateStatus(entryOperation.Id, 6, "Enqueuing to Nonconditional Update");
                  nonconditionalUpdateOperationExecutionContextQueue.Enqueue(executionContext);
                  break;
               default:
                  operationDiagnosticsTable.UpdateStatus(entryOperation.Id, 6, "Unhandled operation type (*)");
                  logger.Error($"Unhandled operation type {executionContext.Operation.Type}");
                  throw new InvalidStateException("Unknown operation type: " + executionContext.Operation.Type + " for operation " + executionContext.Operation.GetType());
            }
            operationDiagnosticsTable.UpdateStatus(entryOperation.Id, 6, "Releasing op available signal (*).");
            operationsAvailableSignal.Release();
            return executionContext;
         }
      }

      public class CacheRequestContext {

      }

      public interface IEntryOperationBinaryLogData {
         Guid EntryOperationId { get; }
      }

      public class EntryOperationBinaryLogData<TResult> : IEntryOperationBinaryLogData, ISerializableType {
         public Guid EntryOperationId => EntryOperation.Id;
         public K Key { get; set; }
         public IEntryOperation<K, V, TResult> EntryOperation { get; set; }
         public AsyncBox<TResult> ResultBox { get; set; }

         public void Serialize(IBodyWriter writer) {
            writer.Write(Key);
            writer.Write(EntryOperation);
         }

         public void Deserialize(IBodyReader reader) {
            Key = reader.Read<K>();
            EntryOperation = reader.Read<IEntryOperation<K, V, TResult>>();
         }
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
         public CacheConfiguration<K, V> CacheConfiguration => Context.CacheConfiguration;
         public PartitioningConfiguration PartitioningConfiguration => Context.PartitioningConfiguration;
         public StaticClusterConfiguration StaticClusterConfiguration => Context.StaticClusterConfiguration;
         public LiveClusterConfiguration LiveClusterConfiguration => Context.LiveClusterConfiguration;
         public SlaveBinaryLogContainer SlaveBinaryLogContainer => Context.SlaveBinaryLogContainer;
         public Partitioner Partitioner => Context.Partitioner;
         public OperationDiagnosticsTable OperationDiagnosticsTable => Context.OperationDiagnosticsTable;
         public ICachePersistenceStrategy<K, V> CachePersistenceStrategy => Context.CachePersistenceStrategy;

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

         public PhaseContext(string contextName, Identity identity, PhaseContext parentPhaseContext, Guid cacheId, CourierFacade courier, ClusterMessenger messenger, CacheConfiguration<K, V> cacheConfiguration, LiveClusterConfiguration liveClusterConfiguration, SlaveBinaryLogContainer slaveBinaryLogContainer, Channel<IEntryOperationExecutionContext> inboundExecutionContextChannel, Partitioner partitioner, OperationDiagnosticsTable operationDiagnosticsTable) {
            this.contextName = contextName;
            this.parentPhaseContext = parentPhaseContext;
            Identity = identity;
            CacheId = cacheId;
            Courier = courier;
            Messenger = messenger;
            Channels = new PhaseContextChannels(identity, inboundExecutionContextChannel);
            CacheConfiguration = cacheConfiguration;
            LiveClusterConfiguration = liveClusterConfiguration;
            SlaveBinaryLogContainer = slaveBinaryLogContainer;
            Partitioner = partitioner;
            OperationDiagnosticsTable = operationDiagnosticsTable;
         }

         public async Task TransitionAsync(PhaseBase nextPhase) {
            try {
               var lastPhase = currentPhase;

               nextPhase.ThrowIfNull(nameof(nextPhase));
               nextPhase.Context = this;
               nextPhase.LastPhase = lastPhase;
               nextPhase.Generation = generationCounter++;

               Log($"Begin transition from ({currentPhase?.Generation ?? -1}) {currentPhase?.Description ?? "[null]"} to ({nextPhase.Generation}) {nextPhase.Description}.");

               if (lastPhase != null) {
                  await lastPhase.HandleLeaveAsync().ConfigureAwait(false);
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
            } catch (Exception e) {
               logger.Error("Transition threw", e);
               throw;
            }
         }

         public Task ForkAsync(string name, PhaseBase nextPhase) {
            var newPhaseContext = new PhaseContext(name, Identity, this, CacheId, Courier, Messenger, CacheConfiguration, LiveClusterConfiguration, SlaveBinaryLogContainer, Channels.InboundExecutionContextChannel, Partitioner, OperationDiagnosticsTable);
            childContexts.AddOrThrow(newPhaseContext);
            Messenger.AddForkOrThrow(newPhaseContext);
            return newPhaseContext.TransitionAsync(nextPhase);
         }

         public void Log(string s) {
            lock (consoleLogLock) {
               Console.BackgroundColor = (ConsoleColor)((uint)Courier.Identity.Id.GetHashCode() % 7);
               var message = $"{Courier.Identity.Id.ToString("n").Substring(0, 6)} [{contextName}]: " + s;
//               Console.WriteLine(message);
//               logger.Info(message);
               Console.BackgroundColor = ConsoleColor.Black;
            }
         }

         public Identity Identity { get; }
         public Guid CacheId { get; }
         public CourierFacade Courier { get; }
         public ClusterMessenger Messenger { get; }
         public PhaseContextChannels Channels { get; }
         public CacheConfiguration<K, V> CacheConfiguration { get; }
         public PartitioningConfiguration PartitioningConfiguration => CacheConfiguration.PartitioningConfiguration;
         public StaticClusterConfiguration StaticClusterConfiguration => CacheConfiguration.StaticClusterConfiguration;
         public LiveClusterConfiguration LiveClusterConfiguration { get; }
         public SlaveBinaryLogContainer SlaveBinaryLogContainer { get; }
         public Partitioner Partitioner { get; set; }
         public OperationDiagnosticsTable OperationDiagnosticsTable { get; }
         public ICachePersistenceStrategy<K, V> CachePersistenceStrategy => CacheConfiguration.CachePersistenceStrategy;

         public Task ProcessInboundMessageAsync<T>(IInboundMessageEvent<T> inboundMessageEvent) {
            return Task.WhenAll(
               Go(async () => {
                  if (typeof(T) == typeof(LeaderHeartBeatDto)) {
                     await Channels.LeaderHeartBeat.WriteAsync((IInboundMessageEvent<LeaderHeartBeatDto>)inboundMessageEvent).ConfigureAwait(false);
                  } else if (typeof(T) == typeof(RepartitionCompleteDto)) {
                     await Channels.RepartitionComplete.WriteAsync((IInboundMessageEvent<RepartitionCompleteDto>)inboundMessageEvent).ConfigureAwait(false);
                  } else if (typeof(T) == typeof(ElectDto)) {
                     Log("Processing inbound elect from: " + inboundMessageEvent.SenderId);
                     await Channels.Elect.WriteAsync((IInboundMessageEvent<ElectDto>)inboundMessageEvent).ConfigureAwait(false);
                     Log("Processed inbound elect from: " + inboundMessageEvent.SenderId);
                  } else if (typeof(T) == typeof(CommitOperationProcessedDto)) {
                     await Channels.CommitOperationProcessed.WriteAsync((IInboundMessageEvent<CommitOperationProcessedDto>)inboundMessageEvent).ConfigureAwait(false);
                  } else {
                     throw new NotSupportedException();
                  }
               }),
               Go(() => Task.WhenAll(childContexts.Select(childContext => childContext.ProcessInboundMessageAsync<T>(inboundMessageEvent)))));
         }
      }

      public class PhaseContextChannels {
         private readonly Identity identity;

         public PhaseContextChannels(Identity identity, Channel<IEntryOperationExecutionContext> inboundExecutionContextChannel) {
            this.identity = identity;
            InboundExecutionContextChannel = inboundExecutionContextChannel;

            var x = new BlockingChannel<IInboundMessageEvent<ElectDto>>();
            if (identity.ToString().Contains("4")) {
//               x.EnableDebug();
            }
            Elect = x;
//            ((dynamic)LeaderHeartBeat).EnableDebug();
         }

         public Channel<IEntryOperationExecutionContext> InboundExecutionContextChannel { get; set; }
         public Channel<IInboundMessageEvent<LeaderHeartBeatDto>> LeaderHeartBeat { get; } = ChannelFactory.Blocking<IInboundMessageEvent<LeaderHeartBeatDto>>();
         public Channel<IInboundMessageEvent<RepartitionCompleteDto>> RepartitionComplete { get; } = ChannelFactory.Blocking<IInboundMessageEvent<RepartitionCompleteDto>>();
         public Channel<IInboundMessageEvent<ElectDto>> Elect { get; } 
         public Channel<IInboundMessageEvent<CommitOperationProcessedDto>> CommitOperationProcessed { get; } = ChannelFactory.Blocking<IInboundMessageEvent<CommitOperationProcessedDto>>();
      }

      public class IndeterminatePhase : PhaseBase {
         public override string Description => "[Indeterminate]";

         public override async Task RunAsync() {
            Log("At indeterminate runasync");
            await new Select {
               Case(Time.After(5000), TransitionToElection),
               Case(Channels.Elect, TransitionToElection),
               Case(Channels.LeaderHeartBeat, FailNotImplemented)
            }.WaitAsync().ConfigureAwait(false);
            Log("Exiting indeterminate runasync");
         }

         private Task TransitionToElection() {
            Log("Transitioning to election");
            IReadOnlySet<Guid> cohortIds = new HashSet<Guid> { Identity.Id };
            return TransitionAsync(new ElectionCandidatePhase(cohortIds));
         }
      }

      public class ElectionCandidatePhase : PhaseBase {
         private const int kDefaultTicksToVictory = 5;

         private readonly IReadOnlySet<Guid> cohortIds;
         private readonly int ticksToVictory;

         public ElectionCandidatePhase(IReadOnlySet<Guid> cohortIds) : this(cohortIds, kDefaultTicksToVictory) { }

         public ElectionCandidatePhase(IReadOnlySet<Guid> cohortIds, int ticksToVictory) {
            this.cohortIds = cohortIds;
            this.ticksToVictory = ticksToVictory;
         }

         public override string Description => $"[ElectionCandidate TTV={ticksToVictory}, Cohorts[{cohortIds.Count}]={cohortIds.Join(", ")})]";

         public override async Task HandleEnterAsync() {
            await base.HandleEnterAsync().ConfigureAwait(false);

            await Messenger.SendToClusterAsync(new ElectDto {
               SenderId = Identity.Id,
               NomineeId = Identity.Id,
               FollowerIds = cohortIds
            }).ConfigureAwait(false);
         }

         public override async Task RunAsync() {
            var loop = true;
            while (IsRunning && loop) {
               loop = false;

               await new Select {
                  Case(Time.After(500), async () => {
                     if (ticksToVictory == 1) {
                        logger.Info("Party time!");
                        await TransitionAsync(new CoordinatorEntryPointPhase(cohortIds)).ConfigureAwait(false);
                     } else {
                        await TransitionAsync(new ElectionCandidatePhase(cohortIds, ticksToVictory - 1)).ConfigureAwait(false);
                     }
                  }),
                  Case(Channels.Elect, async message => {
                     var electDto = message.Body;
                     if (Identity.Id.CompareTo(electDto.NomineeId) < 0) {
                        await TransitionAsync(new ElectionFollowerPhase(electDto.NomineeId)).ConfigureAwait(false);
                     } else {
                        var nextCohortIds = new HashSet<Guid>(cohortIds);
                        if (nextCohortIds.Add(message.SenderId)) {
                           await TransitionAsync(new ElectionCandidatePhase(nextCohortIds, ticksToVictory + 1)).ConfigureAwait(false);
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
            await base.HandleEnterAsync().ConfigureAwait(false);

            Go(async () => {
               Log("Sending election to leader " + leaderId);
               await Messenger.SendToCohortReliableAsync(
                  leaderId,
                  new ElectDto {
                     SenderId = Identity.Id,
                     NomineeId = leaderId,
                     FollowerIds = new HashSet<Guid>()
                  }).ConfigureAwait(false);
               Log("Sent election to leader " + leaderId);
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
                     await TransitionAsync(new IndeterminatePhase()).ConfigureAwait(false);
                  }),
                  Case(Channels.Elect, async message => {
                     last = 2;
                     var electDto = message.Body;
                     if (leaderId.CompareTo(electDto.NomineeId) < 0) {
                        await TransitionAsync(new ElectionFollowerPhase(electDto.NomineeId)).ConfigureAwait(false);
                     } else {
                        loop = true;
                     }
                  }),
                  Case(Channels.LeaderHeartBeat, async x => {
                     last = 3;
                     if (x.Body.CohortIds.Contains(Identity.Id)) {
                        await TransitionAsync(new CohortRepartitionPhase(x.SenderId, x.Body.CohortIds)).ConfigureAwait(false);
                     } else {
                        await FailNotImplemented().ConfigureAwait(false);
                     }
                  })
               };
            }

            if (IsRunning) {
               Console.WriteLine("FOLLOWER CASE " + last);
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
                     LiveClusterConfiguration.LocalRank = Array.IndexOf(rankedCohortIds, Identity.Id);
                     LiveClusterConfiguration.CohortIdsByRank = rankedCohortIds;
                     LiveClusterConfiguration.PartitionGuidsByIndex = Enumerable.Range(0, LiveClusterConfiguration.PartitionCount)
                                                                                .Select(partitionId => SomeHelperClass.AddToGuidSomehow(CacheConfiguration.CacheId, partitionId))
                                                                                .ToArray();
                     LiveClusterConfiguration.AssignedPartitionsIndicesByRank = x.Body.PartitionIdsByRank;
                     LiveClusterConfiguration.AssignedPartitionIndices = Enumerable.Range(0, PartitioningConfiguration.Redundancy)
                                                                                   .Select(i => (i + LiveClusterConfiguration.LocalRank) % LiveClusterConfiguration.CohortCount)
                                                                                   .ToArray();
                     LiveClusterConfiguration.ReplicaCohortRanks = Enumerable.Range(1, PartitioningConfiguration.Redundancy - 1)
                                                                             .Select(i => (LiveClusterConfiguration.LocalRank - i + LiveClusterConfiguration.CohortCount) % LiveClusterConfiguration.CohortCount)
                                                                             .ToArray();
                     LiveClusterConfiguration.ReplicaCohortIds = LiveClusterConfiguration.ReplicaCohortRanks
                                                                                         .Map(cohortRank => LiveClusterConfiguration.CohortIdsByRank[cohortRank]);
                     LiveClusterConfiguration.CohortContextsById = rankedCohortIds.ToDictionary(
                        cohortId => cohortId,
                        cohortId => {
                           var peerContext = PeerTable.GetOrAdd(cohortId);
                           var cohortContext = new CohortContext(
                              new CohortReplicationState(),
                              RemoteServiceProxyContainer.Get<ISomeReplicationService>(peerContext),
                              RemoteServiceProxyContainer.Get<ICacheService<K, V>>(CacheConfiguration.CacheId, peerContext)
                              );
                           return cohortContext;
                        });

                     await TransitionAsync(new CohortMainLoopPhase(leaderId)).ConfigureAwait(false);
                  }),
                  Case(Channels.LeaderHeartBeat, () => {}),
                  Case(Time.After(5000), () => TransitionAsync(new IndeterminatePhase()))
               };
            }
         }
      }

      public class CohortMainLoopPhase : PhaseBase {
         private delegate Task ProcessInboundExecutionContextFunc(CohortMainLoopPhase self, IEntryOperationExecutionContext entryOperation);
         private static readonly IGenericFlyweightFactory<ProcessInboundExecutionContextFunc> processInboundExecutionContextVisitors
             = GenericFlyweightFactory.ForMethod<ProcessInboundExecutionContextFunc>(
                typeof(CohortMainLoopPhase),
                nameof(ProcessInboundExecutionContextVisitor));

         private static Task ProcessInboundExecutionContextVisitor<TResult>(CohortMainLoopPhase self, IEntryOperationExecutionContext executionContext) {
            return self.ProcessInboundExecutionContextAsync((EntryOperationExecutionContext<TResult>)executionContext);
         }

         private delegate Task ProcessCommittedEntryOperationLogDataFunc(CohortMainLoopPhase self, object entryOperationLogData, bool isLocallyLed);
         private static readonly IGenericFlyweightFactory<ProcessCommittedEntryOperationLogDataFunc> enqueueCommittedEntryOperationLogDataForProcessingVisitors
             = GenericFlyweightFactory.ForMethod<ProcessCommittedEntryOperationLogDataFunc>(
                typeof(CohortMainLoopPhase),
                nameof(EnqueueCommittedEntryOperationLogDataForProcessingFuncVisitor));

         private static Task EnqueueCommittedEntryOperationLogDataForProcessingFuncVisitor<TResult>(CohortMainLoopPhase self, object entryOperationLogData, bool isLocallyLed) {
            return self.EnqueueCommittedEntryOperationLogDataForProcessingAndProcessAsynchronously((EntryOperationBinaryLogData<TResult>)entryOperationLogData, isLocallyLed);
         }

         private readonly Guid leaderId;
         private readonly ConcurrentDictionary<K, SomethingToDoWithEntryOperationChuggingAbstractBeanFactorySingleton> somethingToDoWithEntryOperationChuggingAbstractBeanFactorySingletonByKey = new ConcurrentDictionary<K, SomethingToDoWithEntryOperationChuggingAbstractBeanFactorySingleton>();
         private readonly Channel<BinaryLogEntry> ledBinaryLogCommittedEntryChannel = ChannelFactory.Nonblocking<BinaryLogEntry>();
         private readonly BinaryLog ledBinaryLog;
         private readonly AsyncAutoResetLatch ledBinaryLogHasWorkAvailableSignal = new AsyncAutoResetLatch();
         private readonly ConcurrentDictionary<Guid, AsyncCountdownLatch> commitOperationProcessedSignalsByOperationId = new ConcurrentDictionary<Guid, AsyncCountdownLatch>();

         public CohortMainLoopPhase(Guid leaderId) {
            this.leaderId = leaderId;
            ledBinaryLog = new BinaryLog(ledBinaryLogCommittedEntryChannel);
         }

         public override string Description => $"[CohortMainLoop Leader={leaderId}, RankedCohorts[{LiveClusterConfiguration.CohortIdsByRank.Length}]={LiveClusterConfiguration.CohortIdsByRank.Join(", ")}]";

         public override async Task HandleEnterAsync() {
            await base.HandleEnterAsync().ConfigureAwait(false);

            Log("I own partition guid " + LiveClusterConfiguration.LedPartitionId);
            foreach (var cohortId in LiveClusterConfiguration.CohortIdsByRank) {
               Log($"I know about cohort {cohortId}.");
               if (LiveClusterConfiguration.ReplicaCohortIds.Contains(cohortId)) {
                  Log($"Is my slave");
               }
               if (Identity.Id == cohortId) {
                  Log($"Is me");
               }
            }

            foreach (var assignedPartitionIndex in LiveClusterConfiguration.AssignedPartitionIndices) {
               var partitionId = LiveClusterConfiguration.PartitionGuidsByIndex[assignedPartitionIndex];

               if (assignedPartitionIndex == LiveClusterConfiguration.LedPartitionIndex) {
                  StartReplicaLogic(partitionId, ledBinaryLog, ledBinaryLogCommittedEntryChannel, true);
               } else {
                  var committedEntryChannel = ChannelFactory.Nonblocking<BinaryLogEntry>();
                  var slaveBinaryLog = new BinaryLog(
                     committedEntryChannel,
                     async syncedLogEntry => {
                        // Necessary for DB read on prepare and before commit
                        // to avoid race condition where coordinator processes commit,
                        // then replica reads from DB and processes commit (double commit).
                        if (syncedLogEntry.Data is IEntryOperationBinaryLogData) {
                           var key = (K)((dynamic)syncedLogEntry.Data).Key;
                           await GetOrAddAndInstantiateSomethingToDoWithEntryOperationChuggingAbstractBeanFactorySingletonAsync(key).ConfigureAwait(false);
                        }
                     });
                  StartReplicaLogic(partitionId, slaveBinaryLog, committedEntryChannel, false);
               }
            }

            RunLeaderLogicAsync().Forget();
         }

         private void StartReplicaLogic(Guid partitionId, BinaryLog slaveBinaryLog, Channel<BinaryLogEntry> committedEntryChannel, bool isLocallyLed) {
            Log("I am in partition guid " + partitionId);
            SlaveBinaryLogContainer.AddOrThrow(partitionId, slaveBinaryLog);

            Go(async () => {
               while (true) {
                  var entry = await committedEntryChannel.ReadAsync().ConfigureAwait(false);

                  Log($"Processing Committed Entry {entry.Id}.");
                  var data = entry.Data;
                  var dataType = data.GetType();
                  if (dataType.IsGenericType && data is IEntryOperationBinaryLogData) {
                     var tResult = dataType.GetGenericArguments()[2];
                     await enqueueCommittedEntryOperationLogDataForProcessingVisitors.Get(tResult)(this, data, isLocallyLed).ConfigureAwait(false);
                  }
               }
            }).Forget();
         }

         private async Task EnqueueCommittedEntryOperationLogDataForProcessingAndProcessAsynchronously<TResult>(EntryOperationBinaryLogData<TResult> entryOperationLogData, bool isLocallyLed) {
            var entryOperation = entryOperationLogData.EntryOperation;

            bool destroyDiagnosticRow = false;
            if (!isLocallyLed) {
               destroyDiagnosticRow = OperationDiagnosticsTable.TryCreate(
                  entryOperation.Id,
                  entryOperation.GetType().Name + " " + entryOperationLogData.Key,
                  $"Via {nameof(EnqueueCommittedEntryOperationLogDataForProcessingAndProcessAsynchronously)}");
            }

            OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 5, "Processing");
            var somethingToDoWithEntryOperationChuggingAbstractBeanFactorySingleton = await GetOrAddAndInstantiateSomethingToDoWithEntryOperationChuggingAbstractBeanFactorySingletonAsync(entryOperationLogData.Key).ConfigureAwait(false);
            OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 5, "Enqueuing");
            var executionContext = somethingToDoWithEntryOperationChuggingAbstractBeanFactorySingleton.EnqueueOperationAndGetExecutionContext(entryOperationLogData.Key, entryOperation);
            OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 5, "Enqueued");

            Go(async () => {
               await TaskEx.YieldToThreadPool();

               OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 5, "Awaiting Processing");
               var result = await executionContext.ResultBox.GetResultAsync().ConfigureAwait(false);
               entryOperationLogData.ResultBox?.SetResult(result);
               OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 5, "Processed");

               var partitionId = Partitioner.ComputePartitionIndex(entryOperationLogData.Key);
               var partitionMasterCohortId = LiveClusterConfiguration.CohortIdsByRank[partitionId];

               if (partitionMasterCohortId == Identity.Id) {
                  OperationDiagnosticsTable.AppendExtra(entryOperation.Id, "Local Signal");
                  OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 5, "Local Signalling (This is the end)");
                  commitOperationProcessedSignalsByOperationId[entryOperation.Id].Signal();
               } else {
                  OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 5, "Remote Signalling Spawn");
                  Go(async () => {
                     OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 5, "Remote Signalling Entered");
                     await Messenger.SendToCohortReliableAsync(
                        partitionMasterCohortId,
                        new CommitOperationProcessedDto {
                           OperationId = entryOperation.Id
                        }).ConfigureAwait(false);
                     OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 5, "Remote Signalled");

                     if (!isLocallyLed && destroyDiagnosticRow) {
                        OperationDiagnosticsTable.Destroy(entryOperation.Id);
                     }
                  }).Forget();
               }
            }).Forget();
         }

         private async Task<SomethingToDoWithEntryOperationChuggingAbstractBeanFactorySingleton> GetOrAddAndInstantiateSomethingToDoWithEntryOperationChuggingAbstractBeanFactorySingletonAsync(K key) {
            SomethingToDoWithEntryOperationChuggingAbstractBeanFactorySingleton addedInstance = null;
            var result = somethingToDoWithEntryOperationChuggingAbstractBeanFactorySingletonByKey.GetOrAdd(
               key,
               add => {
                  return addedInstance = new SomethingToDoWithEntryOperationChuggingAbstractBeanFactorySingleton(key, CachePersistenceStrategy, OperationDiagnosticsTable);
               });
            if (result == addedInstance) {
               await addedInstance.InitializeAsync().ConfigureAwait(false);
            }
            return result;
         }

         private async Task RunLeaderLogicAsync() {
            await Task.Delay(5000).ConfigureAwait(false);

            // HACK: Make the cluster do something
            //            Go(async () => {
            //               for (int i = 0;; i++) {
            //                  await someBinaryLogThing.AppendAsync(
            //                     new EntryOperationBinaryLogData<Entry<K, V>> {
            //                        EntryOperation = new EntryPutOperation<K, V> {
            //                           Key = (K)(object)(i % 10),
            //                           Value = (V)(object)("value " + i)
            //                        }
            //                     });
            //                  await Task.Delay(10000);
            //               }
            //            }).Forget();

            var slaveCohortContextsById = LiveClusterConfiguration.CohortContextsById
                                                           .Where(kvp => LiveClusterConfiguration.ReplicaCohortIds.Contains(kvp.Key))
                                                           .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            while (true) {
               try {
                  await ledBinaryLogHasWorkAvailableSignal.WaitAsync().ConfigureAwait(false);
                  Log("Entered main loop iteration");

                  // sync log entries
                  foreach (var cohortId in LiveClusterConfiguration.ReplicaCohortIds) {
                     var cohortContext = LiveClusterConfiguration.CohortContextsById[cohortId];

                     var nextEntryIdToSync = cohortContext.ReplicationState.NextEntryIdToSync;
                     var entriesThatNeedToBSynced = await ledBinaryLog.GetAllEntriesFrom(nextEntryIdToSync).ConfigureAwait(false);

                     if (entriesThatNeedToBSynced.Any()) {
                        foreach (var entry in entriesThatNeedToBSynced) {
                           var operationId = (entry.Data as IEntryOperationBinaryLogData)?.EntryOperationId;
                           if (operationId.HasValue) {
                              OperationDiagnosticsTable.UpdateStatus(operationId.Value, 4, "Syncing");
                           }
                        }

                        try {
                           await cohortContext.ReplicationService.SyncAsync(LiveClusterConfiguration.LedPartitionId, entriesThatNeedToBSynced).ConfigureAwait(false);
                           await cohortContext.ReplicationState.UpdateNextEntryIdToSync(entriesThatNeedToBSynced.Last().Id + 1).ConfigureAwait(false);
                           Log($"Got cohort {cohortId.ToShortString()} synced to {entriesThatNeedToBSynced.Last().Id}.");

                           foreach (var entry in entriesThatNeedToBSynced) {
                              var operationId = (entry.Data as IEntryOperationBinaryLogData)?.EntryOperationId;
                              if (operationId.HasValue) {
                                 OperationDiagnosticsTable.UpdateStatus(operationId.Value, 4, "Synced");
                              }
                           }
                        } catch (RemoteException e) {
                           logger.Error("Something bad happened at sync.", e);
                           ledBinaryLogHasWorkAvailableSignal.Set();
                        }
                     }
                  }

                  // advance commit pointer
                  {
                     var greatestFullySyncedEntryId = slaveCohortContextsById.Values.Min(x => x.ReplicationState.NextEntryIdToSync) - 1;
                     var greatestCommittedEntryId = await ledBinaryLog.GetGreatestCommittedEntryId().ConfigureAwait(false);
                     if (greatestFullySyncedEntryId > greatestCommittedEntryId) {
                        var entriesToAdvanceTo = await ledBinaryLog.GetAllEntriesFrom(greatestCommittedEntryId + 1, greatestFullySyncedEntryId).ConfigureAwait(false);

                        foreach (var entry in entriesToAdvanceTo) {
                           var operationId = (entry.Data as IEntryOperationBinaryLogData)?.EntryOperationId;
                           if (operationId.HasValue) {
                              OperationDiagnosticsTable.UpdateStatus(operationId.Value, 4, "Advancing");
                           }
                        }

                        await ledBinaryLog.UpdateGreatestCommittedEntryId(greatestFullySyncedEntryId).ConfigureAwait(false);

                        foreach (var entry in entriesToAdvanceTo) {
                           var operationId = (entry.Data as IEntryOperationBinaryLogData)?.EntryOperationId;
                           if (operationId.HasValue) {
                              OperationDiagnosticsTable.UpdateStatus(operationId.Value, 4, "Advanced");
                           }
                        }
                     }
                  }

                  // sync commit pointer
                  foreach (var kvp in slaveCohortContextsById) {
                     var cohortId = kvp.Key;
                     var cohortContext = kvp.Value;

                     var greatestCommittedEntryId = await ledBinaryLog.GetGreatestCommittedEntryId().ConfigureAwait(false);

                     if (cohortContext.ReplicationState.GreatestCommittedEntryId < greatestCommittedEntryId) {
                        try {
                           await cohortContext.ReplicationService.CommitAsync(LiveClusterConfiguration.LedPartitionId, greatestCommittedEntryId).ConfigureAwait(false);
                           await cohortContext.ReplicationState.UpdateGreatestCommittedEntryId(greatestCommittedEntryId).ConfigureAwait(false);
                           Log($"Got cohort {cohortId.ToShortString()} commit to {greatestCommittedEntryId}.");
//                           Console.Title = ($"Got cohort {cohortId.ToShortString()} commit to {greatestCommittedEntryId}.");
                        } catch (RemoteException e) {
                           logger.Error("Something bad happened at commit.", e);
                           ledBinaryLogHasWorkAvailableSignal.Set();
                        }
                     }
                  }
               } catch (Exception e) {
                  logger.Error("We threw a ", e);
               }
            }
         }

         public override async Task RunAsync() {
            while (IsRunning) {
               await new Select {
                  Case(Channels.LeaderHeartBeat, () => {}),
                  Case(Time.After(500000), () => TransitionAsync(new IndeterminatePhase())),
                  Case(Channels.InboundExecutionContextChannel, x => {
                     var tResult = x.GetType().GetGenericArguments()[2];
                     processInboundExecutionContextVisitors.Get(tResult)(this, x);
                  }),
                  Case(Channels.CommitOperationProcessed, x => {
                     OperationDiagnosticsTable.AppendExtra(x.Body.OperationId, "Signal from " + x.SenderId.ToShortString());
                     var latch = commitOperationProcessedSignalsByOperationId[x.Body.OperationId];
                     ThreadPool.QueueUserWorkItem(_ => latch.Signal());
                  })
               }.WaitAsync().ConfigureAwait(false);
            }
         }

         public async Task ProcessInboundExecutionContextAsync<TResult>(EntryOperationExecutionContext<TResult> executionContext) {
            await TaskEx.YieldToThreadPool();

            var entryOperation = executionContext.Operation;
            OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 2, "Processing Inbound Execution Context");

            var isReadOperation = entryOperation.Type == EntryOperationType.Read;

            var partitionId = Partitioner.ComputePartitionIndex(executionContext.Key);
            if (!LiveClusterConfiguration.AssignedPartitionIndices.Contains(partitionId)) {
               OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 2, "PTP/UA (*)");

               Log("Proxying inbound entry operation to peer (not assigned partition).");
               Log($"(I am assigned {LiveClusterConfiguration.AssignedPartitionIndices.Join(", ")} and key is in partition {partitionId}.");
               await HandleEntryOperationExecutionProxy(executionContext, isReadOperation).ConfigureAwait(false);
            } else if (LiveClusterConfiguration.LedPartitionIndex != partitionId && !isReadOperation) {
               OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 2, "PTP/DO (*)");

               Log("Proxying inbound entry operation to peer (don't own partition).");
               await HandleEntryOperationExecutionProxy(executionContext, false).ConfigureAwait(false);
            } else if (isReadOperation) {
               OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 2, "HRL");
               Log("Handling read operation locally.");
               OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 2, "HRL GOA");
               var somethingToDoWithEntryOperationChuggingAbstractBeanFactorySingleton = await GetOrAddAndInstantiateSomethingToDoWithEntryOperationChuggingAbstractBeanFactorySingletonAsync(executionContext.Key).ConfigureAwait(false);
               OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 2, "HRL Enqueuing");
               var enqueuedExecutionContext = somethingToDoWithEntryOperationChuggingAbstractBeanFactorySingleton.EnqueueOperationAndGetExecutionContext(executionContext.Key, entryOperation);
               OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 2, "HRL Processing");
               var result = await enqueuedExecutionContext.ResultBox.GetResultAsync().ConfigureAwait(false);
               OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 2, "HRL Processed (*)");
               executionContext.ResultBox.SetResult(result);
            } else {
               OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 2, "ATL");
               
               // Necessary for DB read on prepare and before commit
               // to avoid race condition where replica processes commit,
               // then coordinator reads from DB and processes commit (double commit).
               await GetOrAddAndInstantiateSomethingToDoWithEntryOperationChuggingAbstractBeanFactorySingletonAsync(executionContext.Key).ConfigureAwait(false);

               // append entry to binary log, processing it will set the resultbox value.
               var nodeCommitOperationsProcessedSignal = new AsyncCountdownLatch(PartitioningConfiguration.Redundancy);
               var operationId = entryOperation.Id;
               commitOperationProcessedSignalsByOperationId.AddOrThrow(operationId, nodeCommitOperationsProcessedSignal);

               var intermediateResultBox = new AsyncBox<TResult>();

               var entryOperationLogData = new EntryOperationBinaryLogData<TResult> {
                  Key = executionContext.Key,
                  EntryOperation = entryOperation,
                  ResultBox = intermediateResultBox
               };

               OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 2, "ATL Appending");
               await ledBinaryLog.AppendAsync(entryOperationLogData).ConfigureAwait(false);
               OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 2, "ATL Appended");

               ledBinaryLogHasWorkAvailableSignal.Set();

               Log("Appended new entry to binary log.");

               OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 2, "ATL Spawning Go");
               Go(async () => {
                  OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 2, "ATL Awaiting Processed");
                  await nodeCommitOperationsProcessedSignal.WaitAsync().ConfigureAwait(false);

                  OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 2, "ATL Awaiting Result");
                  var result = await intermediateResultBox.GetResultAsync().ConfigureAwait(false);

                  OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 2, "ATL Signalling Completion (This is the end)");
                  executionContext.ResultBox.SetResult(result);
               }).Forget();
            }
         }

         private async Task HandleEntryOperationExecutionProxy<TResult>(EntryOperationExecutionContext<TResult> executionContext, bool includeProxyToSlaves) {
            var entryOperation = executionContext.Operation;
            OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 3, "Proxy Begin");
            var partitionId = Partitioner.ComputePartitionIndex(executionContext.Key);
            var cohortRankOffset = includeProxyToSlaves ? StaticRandom.Next(PartitioningConfiguration.Redundancy) : 0;
            int peerRankToDispatchTo = (partitionId - cohortRankOffset + LiveClusterConfiguration.CohortCount) % LiveClusterConfiguration.CohortCount;
            var peerId = LiveClusterConfiguration.CohortIdsByRank[peerRankToDispatchTo];
            var peerCacheService = LiveClusterConfiguration.CohortContextsById[peerId].CacheService;
            OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 3, $"Proxy RMI Dispatch [{peerRankToDispatchTo}]");
            var result = await peerCacheService.ProcessEntryOperationAsync(executionContext.Key, entryOperation).ConfigureAwait(false);
            OperationDiagnosticsTable.UpdateStatus(entryOperation.Id, 3, $"Proxy RMI Responded [{peerRankToDispatchTo}] (this is the end)");
            executionContext.ResultBox.SetResult(result);
         }
      }

      [AutoSerializable]
      public class CohortContext {
         // For Vox Deserialization Only!
         public CohortContext() { }

         public CohortContext(CohortReplicationState replicationState, ISomeReplicationService replicationService, ICacheService<K, V> cacheService) {
            ReplicationState = replicationState;
            ReplicationService = replicationService;
            CacheService = cacheService;
         }

         // HACK: private set is for autoserialization
         // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
         public CohortReplicationState ReplicationState { get; private set; }
         public ISomeReplicationService ReplicationService { get; }
         public ICacheService<K, V> CacheService { get; }
      }

      public class CoordinatorEntryPointPhase : PhaseBase {
         private readonly IReadOnlySet<Guid> cohortIds;
         private Task heartBeatTask;

         public CoordinatorEntryPointPhase(IReadOnlySet<Guid> cohortIds) {
            this.cohortIds = cohortIds;
         }

         public override string Description => $"[CoordinatorEntryPoint Cohorts[{cohortIds.Count}]={cohortIds.Join(", ")}]";

         public override async Task HandleEnterAsync() {
            await base.HandleEnterAsync().ConfigureAwait(false);

            await Context.ForkAsync("FORK", new CohortRepartitionPhase(Identity.Id, cohortIds)).ConfigureAwait(false);

            heartBeatTask = Go(async () => {
               try {
                  logger.Log(LogLevel.Fatal, "Entered leader heartbeat task for " + Description);
                  while (true) {
                     await Messenger.SendToClusterAsync(new LeaderHeartBeatDto {
                        CohortIds = cohortIds
                     }).ConfigureAwait(false);
                     Log("Sent leader heartbeat!");
                     await Task.Delay(500).ConfigureAwait(false);
                  }
               } catch (Exception e) {
                  logger.Error("Heartbeat task died", e);
                  throw;
               }
            });

            // repartition logic
            var rankedCohorts = new SortedSet<Guid>(cohortIds).ToArray();
            foreach (var cohort in rankedCohorts) {
               var cohortPeerContext = PeerTable.GetOrAdd(cohort);
               var haves = RemoteServiceProxyContainer.Get<ISomeRepartitioningService>(cohortPeerContext);
            }

            // divvy up responsibility for data partitions
            LiveClusterConfiguration.LocalRank = Array.IndexOf(rankedCohorts, Identity.Id);
            LiveClusterConfiguration.CohortIdsByRank = rankedCohorts;
            var partitionIdsByRank = new SCG.Dictionary<int, IReadOnlySet<int>>();
            foreach (var cohortRank in Enumerable.Range(0, rankedCohorts.Length)) {
               partitionIdsByRank.Add(
                  cohortRank,
                  new HashSet<int>(
                     Enumerable.Range(0, PartitioningConfiguration.Redundancy)
                               .Select(j => (cohortRank + j) % rankedCohorts.Length)
                     ));
            }
            LiveClusterConfiguration.AssignedPartitionsIndicesByRank = partitionIdsByRank;

            await Messenger.SendToClusterReliableAsync(cohortIds, new RepartitionCompleteDto(rankedCohorts, partitionIdsByRank)).ConfigureAwait(false);
            await TransitionAsync(new CoordinatorMainLoopPhase(cohortIds)).ConfigureAwait(false);
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
//            while (true) {
//               await TaskEx.YieldToThreadPool();
//            }
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

      //         private async Task ProcessEntryUpdateDtoAsync(EntryUpdateDto message) {
      //            throw new NotImplementedException();
      //         }

      public class Partitioner {
         private const int kHashBitCount = 32;
         private readonly PartitioningConfiguration staticConfiguration;
         private readonly LiveClusterConfiguration liveClusterConfiguration;

         public Partitioner(PartitioningConfiguration staticConfiguration, LiveClusterConfiguration liveClusterConfiguration) {
            this.staticConfiguration = staticConfiguration;
            this.liveClusterConfiguration = liveClusterConfiguration;
         }

         public int ComputeBlockId(K key) {
            var hash = JenkinsHashMix((uint)key.GetHashCode());
            
            return (int)(hash >> (kHashBitCount - staticConfiguration.BlockCountPower));
         }

         public int ComputePartitionIndex(K key) => ComputePartitionIndex(ComputeBlockId(key));

         public int ComputePartitionIndex(int blockId) {
            return ComputePartitionIndexWithBlockCount(blockId, staticConfiguration.DerivedBlockCount);
         }

         public int ComputePartitionIndexWithBlockCount(int blockId, int blockCount) {
            int blocksPerPartition = blockCount / liveClusterConfiguration.PartitionCount;
            return blockId / blocksPerPartition;
         }

         public bool IsLocallyMasteredKey(K key) => IsLocallyMasteredPartition(ComputePartitionIndex(key));

         public bool IsLocallyMasteredPartition(int partition) {
            var partitionIdDifference = partition - liveClusterConfiguration.LocalRank;
            if (partitionIdDifference < 0) {
               partitionIdDifference += liveClusterConfiguration.PartitionCount;
            }
            return partitionIdDifference < staticConfiguration.Redundancy;
         }

         /// <summary>
         /// Robert Jenkins' 32 bit integer hash function.
         /// 
         /// See http://www.cris.com/~Ttwang/tech/inthash.htm 
         /// 
         /// Discovered via https://gist.github.com/badboy/6267743 
         ///            and http://burtleburtle.net/bob/hash/integer.html
         /// 
         /// Licensed under Public Domain.
         /// </summary>
         /// <param name="hash"></param>
         /// <returns></returns>
         [MethodImpl(MethodImplOptions.AggressiveInlining)]
         private static uint JenkinsHashMix(uint hash) {
            hash = (hash + 0x7ed55d16) + (hash << 12);
            hash = (hash ^ 0xc761c23c) ^ (hash >> 19);
            hash = (hash + 0x165667b1) + (hash << 5);
            hash = (hash + 0xd3a2646c) ^ (hash << 9);
            hash = (hash + 0xfd7046c5) + (hash << 3);
            hash = (hash ^ 0xb55a4f09) ^ (hash >> 16);
            return hash;
         }
      }
   }
}

