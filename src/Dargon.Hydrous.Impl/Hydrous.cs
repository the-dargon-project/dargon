using Dargon.Commons;
using Fody.Constructors;
using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Courier;

namespace Dargon.Hydrous.Impl {
   public class Program {
      public static void Main(string[] args) {
      }
   }

   public class CacheRoot<K, V> {
      public class ElectDto {
         public Guid ElecteeId { get; set; }
      }

      public class LeaderHeartBeatDto {

      }

      public class RepartitionHaltDto {

      }

      public class RepartitionCompleteDto {

      }

      public class RequestDto {
         public IEntryOperation Operation { get; set; }
      }

      public class EntryUpdateDto {

      }

      public class StaticConfiguration {
         /// <summary>
         /// Default: 10 to get 2^10 Blocks.
         /// The top BlockCountPower bits of a key's hash determine
         /// its blockId.
         /// </summary>
         public int BlockCountPower { get; set; } = 10;

         /// <summary>
         /// Replication Factor.
         /// Default: 2: Entries are mirrored on two cohorts at a time.
         /// </summary>
         public int Redundancy { get; set; } = 2;

         public Guid LocalId { get; set; } = Guid.NewGuid();
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

      public interface IEntryOperation {
         K Key { get; }
      }

      public interface IMessenger {
         Task SendToCluster<T>(T val);
         Task SendToCohort<T>(Guid dest, T val);
      }

      [RequiredFieldsConstructor]
      public class EntryOperationProcesserService {
         private readonly Synchronizer synchronizer = null;
         private readonly Partitioner partitioner = null;
         private readonly LocalCache[] blockCaches = null;
         private int requestsInProgress = 0;

         public void StartProcessingRequest(RequestDto request) {
            Interlocked.Increment(ref requestsInProgress);
            ProcessRequestAsync(request.Operation).Forget();
         }

         internal async Task ProcessRequestAsync(IEntryOperation entryOperation) {
            try {
               using (await synchronizer.Small()) {
                  var key = entryOperation.Key;
                  if (partitioner.IsLocallyMasteredKey(key)) {
                  } else {
                  }
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
         private readonly AsyncSemaphore inboundMessageSignal = new AsyncSemaphore(0);
         private readonly StaticConfiguration staticConfiguration;
         private readonly EntryOperationProcesserService entryOperationProcessorService = null;
         private readonly IMessenger messenger = null;

         public void EnqueueMessage(IInboundMessageEvent<object> message) {
            message.AddRef();

            inboundMessages.Enqueue(message);
            inboundMessageSignal.Release();
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

         public async Task<IInboundMessageEvent<T>> AwaitMessage<T>(int timeoutMillis) where T : class {
            using (var timeoutCts = new CancellationTokenSource(timeoutMillis)) {
               try {
                  while (true) {
                     const bool dontAcceptFromStash = false;
                     var message = await TakeNextMessage(dontAcceptFromStash, timeoutCts.Token);
                     if (message.Body is T) {
                        return (IInboundMessageEvent<T>)message;
                     } else {
                        stashedMessages.Enqueue(message);
                     }
                  }
               } catch (OperationCanceledException) {
                  return null;
               }
            }
         }

         public async Task IndeterminantPhaseAsync() {
            // Indeterminant Phase: Wait for leader heartbeat.
            var leaderHeartBeatEvent = await AwaitMessage<LeaderHeartBeatDto>(5000);
            if (leaderHeartBeatEvent == null) {
               await ElectionPhaseAsync();
            } else {
               leaderHeartBeatEvent.ReleaseRef();
            }
         }

         public async Task ElectionPhaseAsync() {
            while (true) {
               await messenger.SendToCluster(new ElectDto {
                  ElecteeId = staticConfiguration.LocalId
               });
            }
         }

         public async Task InClusterLoopAsync() {
            while (true) {
               const bool doAcceptFromStash = true;
               var message = await TakeNextMessage(doAcceptFromStash);
               switch (message.Body.GetType().Name) {
                  case nameof(RepartitionHaltDto):
                     await RepartitionLoopAsync((RepartitionHaltDto)message);
                     break;
                  case nameof(RepartitionCompleteDto):
                     throw new InvalidOperationException();
                  case nameof(RequestDto):
                     entryOperationProcessorService.StartProcessingRequest((RequestDto)message);
                     break;
                  case nameof(EntryUpdateDto):
                     ProcessEntryUpdateDtoAsync((EntryUpdateDto)message).Forget();
                     break;
               }
            }
         }

         public async Task RepartitionLoopAsync(RepartitionHaltDto repartitionHaltDto) {
            // Block until all locally executing requests have been completed
            var waitForRequestTask = entryOperationProcessorService.WaitForRequestCompletionsAsync(); ;
            while (!waitForRequestTask.IsCompleted) {
               const bool dontAcceptFromStash = true;
               var message = await TakeNextMessage(dontAcceptFromStash);
               switch (message.GetType().Name) {
                  case nameof(RepartitionHaltDto):
                  case nameof(RepartitionCompleteDto):
                     throw new InvalidOperationException();
                  case nameof(RequestDto):
                     stashedMessages.Enqueue(message);
                     break;
                  case nameof(EntryUpdateDto):
                     ProcessEntryUpdateDtoAsync((EntryUpdateDto)message).Forget();
                     break;
               }
            }

            // Send block updates to destination cohorts
//            await messenger.SendToCohort();

            // Send completion signal to coordinator

         }

         private async Task ProcessEntryUpdateDtoAsync(EntryUpdateDto message) {
            throw new NotImplementedException();
         }
      }

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
            int blockCount = 1 << staticConfiguration.BlockCountPower;
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
