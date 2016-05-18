using Dargon.Commons;
using Dargon.Commons.Channels;
using Dargon.Courier;
using Dargon.Courier.AsyncPrimitives;
using Dargon.Courier.TransitTier;
using Dargon.Ryu;
using Dargon.Vox;
using Fody.Constructors;
using Nito.AsyncEx;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static Dargon.Commons.Channels.ChannelsExtensions;

namespace Dargon.Hydrous.Impl {
   public class Program {
      public static void Main(string[] args) {
         InitializeLogging();
         CacheRoot<int, string>.Createsiodkdfajasoif();
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
      }
   }

   [AutoSerializable]
   public class ElectDto {
      public Guid NomineeId { get; set; }
      public HashSet<Guid> FollowerIds { get; set; }
   }

   [AutoSerializable]
   public class LeaderHeartBeatDto { }

   [AutoSerializable]
   public class RepartitionHaltDto { }

   [AutoSerializable]
   public class RepartitionCompleteDto { }

   [AutoSerializable]
   public class RequestDto<K, V> {
      public IEntryOperation<K, V> Operation { get; set; }
   }

   public interface IEntryOperation<K, V> {
      K Key { get; }
   }

   [AutoSerializable]
   public class EntryUpdateDto { }

   public class CacheRoot<K, V> {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      public static void Createsiodkdfajasoif() {
         var transport = new TestTransport();
         Create(transport);
         Create(transport);
         Create(transport);
         Create(transport);
         while (true) ;
      }

      public static void Create(ITransport transport) {
         var root = new RyuFactory().Create();
         var courier = new CourierContainerFactory(root).Create(transport);
         var identity = courier.GetOrThrow<Identity>();
         var router = courier.GetOrThrow<InboundMessageRouter>();
         var staticConfiguration = new StaticConfiguration { LocalId = identity.Id };
         var messenger = new CourierMessenger(courier.GetOrThrow<Messenger>());
         var program = new Program(staticConfiguration, messenger);
         router.RegisterHandler<ElectDto>(x => program.ProcessMessageAsync(x));
         router.RegisterHandler<LeaderHeartBeatDto>(x => program.ProcessMessageAsync(x));
         router.RegisterHandler<RepartitionHaltDto>(x => program.ProcessMessageAsync(x));
         router.RegisterHandler<RepartitionCompleteDto>(x => program.ProcessMessageAsync(x));
         router.RegisterHandler<RequestDto<K, V>>(x => program.ProcessMessageAsync(x));
         router.RegisterHandler<EntryUpdateDto>(x => program.ProcessMessageAsync(x));
         program.IndeterminantPhaseAsync().Forget();
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
         public Dictionary<int, HashSet<int>> PartitionsIdsByRank { get; set; }
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

      private class CourierMessenger : IMessenger {
         private readonly Messenger messenger;

         public CourierMessenger(Messenger messenger) {
            this.messenger = messenger;
         }

         public Task SendToCluster<T>(T val) {
            return messenger.BroadcastAsync(val);
         }

         public Task SendToCohortReliable<T>(Guid dest, T val) {
            return messenger.SendReliableAsync(val, dest);
         }

         public Task SendToCohortUnreliable<T>(Guid dest, T val) {
            return messenger.SendAsync(val, dest);
         }
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

      [RequiredFieldsConstructor]
      public class Program {
         private readonly ConcurrentQueue<IInboundMessageEvent<object>> inboundMessages = new ConcurrentQueue<IInboundMessageEvent<object>>();
         private readonly ConcurrentQueue<IInboundMessageEvent<object>> stashedMessages = new ConcurrentQueue<IInboundMessageEvent<object>>();
         private readonly Channel<IInboundMessageEvent<LeaderHeartBeatDto>> leaderHeartBeatsChannel = ChannelFactory.Blocking<IInboundMessageEvent<LeaderHeartBeatDto>>();
         private readonly Channel<IInboundMessageEvent<ElectDto>> electChannel = ChannelFactory.Blocking<IInboundMessageEvent<ElectDto>>();
         private readonly ConcurrentDictionary<IInboundMessageEvent<object>, AsyncLatch> completionLatchesByInboundMessageEvent = new ConcurrentDictionary<IInboundMessageEvent<object>, AsyncLatch>();
         private readonly AsyncSemaphore inboundMessageSignal = new AsyncSemaphore(0);
         private readonly EntryOperationProcesserService entryOperationProcessorService = null;
         private readonly StaticConfiguration staticConfiguration;
         private readonly IMessenger messenger;

         public Program(StaticConfiguration staticConfiguration, IMessenger messenger) {
            this.staticConfiguration = staticConfiguration;
            this.messenger = messenger;
         }

         public async Task ProcessMessageAsync(IInboundMessageEvent<object> message) {
            var completionLatch = new AsyncLatch();
            completionLatchesByInboundMessageEvent.TryAdd(message, completionLatch);

            if (message.Body is LeaderHeartBeatDto) {
               await leaderHeartBeatsChannel.WriteAsync((IInboundMessageEvent<LeaderHeartBeatDto>)message);
            } else if (message.Body is ElectDto) {
               await electChannel.WriteAsync((IInboundMessageEvent<ElectDto>)message);
            } else {
               inboundMessages.Enqueue(message);
               inboundMessageSignal.Release();
            }

            await completionLatch.WaitAsync();

            AsyncLatch qualityCode;
            completionLatchesByInboundMessageEvent.TryRemove(message, out qualityCode);
         }

         public void Complete(IInboundMessageEvent<object> e) {
            completionLatchesByInboundMessageEvent[e].Set();
         }

         public async Task<IInboundMessageEvent<object>> TakeNextMessage(bool acceptStashed, CancellationToken token = default(CancellationToken)) {
            IInboundMessageEvent<object> message;

            // Messages can't be stashed during the time this method is called
            if (acceptStashed && stashedMessages.TryDequeue(out message)) {
               // Taken from stash.
            } else {
               // Spinwait until reader task writes to inbound message queue.
               await inboundMessageSignal.WaitAsync(token);
               var messageDequeued = inboundMessages.TryDequeue(out message);
               Trace.Assert(messageDequeued);
            }
            return message;
         }

         public async Task<InboundMessageEvent<T>> AwaitMessage<T>(int timeoutMillis) where T : class {
            using (var timeoutCts = new CancellationTokenSource(timeoutMillis)) {
               try {
                  while (true) {
                     const bool dontAcceptFromStash = false;
                     var message = await TakeNextMessage(dontAcceptFromStash, timeoutCts.Token);
                     if (message.Body is T) {
                        return (InboundMessageEvent<T>)message;
                     } else {
                        stashedMessages.Enqueue(message);
                     }
                  }
               } catch (OperationCanceledException) {
                  return null;
               }
            }
         }

         private string ___from = "";

         private void TransitionDebug(string to) {
            logger.Info(staticConfiguration.LocalId.ToString("n").Substring(0, 8) + ": " + ___from + " => " + to);
            ___from = to;
         }

         public async Task IndeterminantPhaseAsync() {
            TransitionDebug("[Indeterminant]");

            // Indeterminant Phase: Wait for leader heartbeat.
            await new Select {
               Case(Time.After(5000), () => ElectionCandidatePhaseAsync(10, new HashSet<Guid> { staticConfiguration.LocalId })),
               Case(electChannel, () => ElectionCandidatePhaseAsync(10, new HashSet<Guid> { staticConfiguration.LocalId })),
               Case(leaderHeartBeatsChannel, async leaderHeartBeatEvent => {
                  await Task.Yield();
                  throw new NotImplementedException();
               })
            };
         }

         public async Task ElectionCandidatePhaseAsync(int ticksToVictory, HashSet<Guid> followerIds) {
            TransitionDebug($"[Candidate TTV={ticksToVictory}, Followers={followerIds.Join(", ")}]");

            messenger.SendToCluster(new ElectDto {
               NomineeId = staticConfiguration.LocalId,
               FollowerIds = followerIds
            }).Forget();

            var loop = true;
            while (loop) {
               loop = false;

               await new Select {
                  Case(Time.After(500), async () => {
                     if (ticksToVictory == 1) {
                        logger.Info("Party time!");
                        await Task.FromResult(false);
                     } else {
                        await ElectionCandidatePhaseAsync(ticksToVictory - 1, followerIds);
                     }
                  }),
                  Case(electChannel, async message => {
                     var electDto = message.Body;
                     if (staticConfiguration.LocalId.CompareTo(electDto.NomineeId) < 0) {
                        await ElectionFollowerAsync(electDto.NomineeId);
                     } else if (followerIds.Add(message.SenderId)) {
                        await ElectionCandidatePhaseAsync(ticksToVictory + 1, followerIds);
                     } else {
                        loop = true;
                     }
                  })
               };
            }
         }

         public async Task ElectionFollowerAsync(Guid leaderId) {
            TransitionDebug($"[Follower LID={leaderId}]");

            messenger.SendToCohortReliable(leaderId, new ElectDto {
               NomineeId = leaderId,
               FollowerIds = new HashSet<Guid>()
            }).Forget();

            var loop = true;
            while (loop) {
               loop = false;

               await new Select {
                  Case(Time.After(5000), IndeterminantPhaseAsync),
                  Case(electChannel, async message => {
                     var electDto = message.Body;
                     if (leaderId.CompareTo(electDto.NomineeId) < 0) {
                        await ElectionFollowerAsync(electDto.NomineeId);
                     } else {
                        loop = true;
                     }
                  })
               };
            }
         }

         //
         //         public async Task InClusterLoopAsync() {
         //            while (true) {
         //               const bool doAcceptFromStash = true;
         //               var message = await TakeNextMessage(doAcceptFromStash);
         //               switch (message.Body.GetType().Name) {
         //                  case nameof(RepartitionHaltDto):
         //                     await RepartitionLoopAsync((RepartitionHaltDto)message.Body);
         //                     break;
         //                  case nameof(RepartitionCompleteDto):
         //                     throw new InvalidOperationException();
         //                  case nameof(RequestDto<K, V>):
         //                     entryOperationProcessorService.StartProcessingRequest((RequestDto<K, V>)message.Body);
         //                     break;
         //                  case nameof(EntryUpdateDto):
         //                     ProcessEntryUpdateDtoAsync((EntryUpdateDto)message.Body).Forget();
         //                     break;
         //               }
         //            }
         //         }
         //
         //         public async Task RepartitionLoopAsync(RepartitionHaltDto repartitionHaltDto) {
         //            // Block until all locally executing requests have been completed
         //            var waitForRequestTask = entryOperationProcessorService.WaitForRequestCompletionsAsync(); ;
         //            while (!waitForRequestTask.IsCompleted) {
         //               const bool dontAcceptFromStash = true;
         //               var message = await TakeNextMessage(dontAcceptFromStash);
         //               switch (message.GetType().Name) {
         //                  case nameof(RepartitionHaltDto):
         //                  case nameof(RepartitionCompleteDto):
         //                     throw new InvalidOperationException();
         //                  case nameof(RequestDto<K, V>):
         //                     stashedMessages.Enqueue(message);
         //                     break;
         //                  case nameof(EntryUpdateDto):
         //                     ProcessEntryUpdateDtoAsync((EntryUpdateDto)message.Body).Forget();
         //                     break;
         //               }
         //            }

         // Send block updates to destination cohorts
         //            await messenger.SendToCohort();

         // Send completion signal to coordinator

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
