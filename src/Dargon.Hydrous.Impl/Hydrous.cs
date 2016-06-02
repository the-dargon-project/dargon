﻿using Dargon.Commons;
using Dargon.Commons.Channels;
using Dargon.Commons.Collections;
using Dargon.Commons.Exceptions;
using Dargon.Courier;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.ServiceTier;
using Dargon.Courier.ServiceTier.Client;
using Dargon.Courier.TransportTier.Udp;
using Dargon.Ryu;
using Dargon.Vox;
using Fody.Constructors;
using Nito.AsyncEx;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Courier.Vox;
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
   public class RepartitionCompleteDto { }

   [AutoSerializable]
   public class RequestDto<K, V> {
      public IEntryOperation<K, V> Operation { get; set; }
   }

   [AutoSerializable]
   public class HaveDto {
      public int EpochId { get; set; }
      public int BlockId { get; set; }
   }

   public interface IEntryOperation<K, V> {
      K Key { get; }
   }

   [AutoSerializable]
   public class EntryUpdateDto { }

   public class CacheRoot<K, V> {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      public static void StartLocalCluster() {
         Func<CourierFacade> buildCourier = () => CourierBuilder.Create()
                                                                .UseUdpMulticastTransport()
                                                                .BuildAsync().Result;
         StartNewCohort(buildCourier);
         StartNewCohort(buildCourier);
         StartNewCohort(buildCourier);
         StartNewCohort(buildCourier);
         new CountdownEvent(1).Wait();
      }

      public static void StartNewCohort(Func<CourierFacade> buildCourier) {
         var root = new RyuFactory().Create();
         var courier = buildCourier();
         var identity = courier.Identity;
         var router = courier.InboundMessageRouter;
         var staticConfiguration = new StaticConfiguration { LocalId = identity.Id };
         var program = new Program(staticConfiguration, courier);

         var clusterMessenger = new ClusterMessenger(courier);

         var phaseContext = new PhaseContext("MAIN", null, courier, clusterMessenger);
         phaseContext.TransitionAsync(new IndeterminatePhase()).Forget();

         router.RegisterHandler<ElectDto>(SomeCloningProxy<ElectDto>(phaseContext));
         router.RegisterHandler<LeaderHeartBeatDto>(SomeCloningProxy<LeaderHeartBeatDto>(phaseContext));
         router.RegisterHandler<RepartitionHaltDto>(SomeCloningProxy<RepartitionHaltDto>(phaseContext));
         router.RegisterHandler<RepartitionCompleteDto>(SomeCloningProxy<RepartitionCompleteDto>(phaseContext));
         router.RegisterHandler<RequestDto<K, V>>(SomeCloningProxy<RequestDto<K, V>>(phaseContext));
         router.RegisterHandler<EntryUpdateDto>(SomeCloningProxy<EntryUpdateDto>(phaseContext));

         var someRepartitioningService = new SomeRepartitioningService();
         courier.LocalServiceRegistry.RegisterService<ISomeRepartitioningService>(someRepartitioningService);
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
         public SCG.Dictionary<int, HashSet<int>> PartitionsIdsByRank { get; set; }
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

      public interface IMessenger {
         Task SendToCluster<T>(T val);
         Task SendToCohortReliable<T>(Guid dest, T val);
         Task SendToCohortUnreliable<T>(Guid dest, T val);
      }

      [RequiredFieldsConstructor]
      public class EntryOperationProcesserService {
         private readonly Synchronizer synchronizer = null;
         private readonly Partitioner partitioner = null;
         private readonly LocalCache[] blockCaches = null;
         private int requestsInProgress = 0;

         public void StartProcessingRequest(RequestDto<K, V> request) {
            Interlocked.Increment(ref requestsInProgress);
            ProcessRequestAsync(request.Operation).Forget();
         }

         internal async Task ProcessRequestAsync(IEntryOperation<K, V> entryOperation) {
            try {
               using (await synchronizer.Small()) {
                  var key = entryOperation.Key;
                  if (partitioner.IsLocallyMasteredKey(key)) { } else { }
               }
            } finally {
               Interlocked.Decrement(ref requestsInProgress);
            }
         }

         public async Task WaitForRequestCompletionsAsync() {
            while (requestsInProgress != 0) {
               await Task.Yield();
            }
         }
      }

      public class SomeEntryContainerThing {

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
         public CourierFacade Courier => Context.Courier;
         public ClusterMessenger Messenger => Context.Messenger;
         public Identity Identity => Courier.Identity;
         public PeerTable PeerTable => Courier.PeerTable;
         public RemoteServiceProxyContainer RemoteServiceProxyContainer => Courier.RemoteServiceProxyContainer;
         public PhaseContextChannels Channels => Context.Channels;

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

         public PhaseContext(string contextName, PhaseContext parentPhaseContext, CourierFacade courier, ClusterMessenger messenger) {
            this.contextName = contextName;
            this.parentPhaseContext = parentPhaseContext;
            Courier = courier;
            Messenger = messenger;
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
            var newPhaseContext = new PhaseContext(name, this, Courier, Messenger);
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

         public CourierFacade Courier { get; }
         public ClusterMessenger Messenger { get; }
         public PhaseContextChannels Channels { get; } = new PhaseContextChannels();

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
         private const int kDefaultTicksToVictory = 10;

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
                  Case(Channels.RepartitionComplete, () => TransitionAsync(new CohortMainLoopPhase(leaderId, cohortIds))),
                  Case(Channels.LeaderHeartBeat, () => {}),
                  Case(Time.After(5000), () => TransitionAsync(new IndeterminatePhase()))
               };
            }
         }
      }

      public class CohortMainLoopPhase : PhaseBase {
         private readonly Guid leaderId;
         private readonly IReadOnlySet<Guid> cohortIds;

         public CohortMainLoopPhase(Guid leaderId, IReadOnlySet<Guid> cohortIds) {
            this.leaderId = leaderId;
            this.cohortIds = cohortIds;
         }

         public override string Description => $"[CohortMainLoop Leader={leaderId}, Cohorts[{cohortIds.Count}]={cohortIds.Join(", ")}]";

         public override async Task RunAsync() {
            while (IsRunning) {
               await new Select {
                  Case(Channels.LeaderHeartBeat, () => {}),
                  Case(Time.After(5000), () => TransitionAsync(new IndeterminatePhase()))
               };
            }
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

            var rankedCohorts = new SortedSet<Guid>(cohortIds).ToArray();
            foreach (var cohort in rankedCohorts) {
               var cohortPeerContext = PeerTable.GetOrAdd(cohort);
               var haves = RemoteServiceProxyContainer.Get<ISomeRepartitioningService>(cohortPeerContext);
            }

            await Messenger.SendToClusterReliableAsync(cohortIds, new RepartitionCompleteDto());
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

      [RequiredFieldsConstructor]
      public class Program {
         private readonly StaticConfiguration staticConfiguration;
         private readonly CourierFacade courier;
         private int currentEpochId = -1;

         public Program(StaticConfiguration staticConfiguration, CourierFacade courier) {
            this.staticConfiguration = staticConfiguration;
            this.courier = courier;
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

      //         private async Task ProcessEntryUpdateDtoAsync(EntryUpdateDto message) {
      //            throw new NotImplementedException();
      //         }

      [RequiredFieldsConstructor]
      public class Partitioner {
         private const int kHashBitCount = 32;
         private readonly StaticConfiguration staticConfiguration = null;
         private readonly LiveConfiguration liveConfiguration = null;

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
