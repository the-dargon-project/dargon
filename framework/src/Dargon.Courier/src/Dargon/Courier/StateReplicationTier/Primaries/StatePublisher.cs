using System;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.AsyncAwait;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Channels;
using Dargon.Commons.Utilities;
using Dargon.Courier.PubSubTier.Publishers;
using Dargon.Courier.StateReplicationTier.States;
using Dargon.Courier.StateReplicationTier.Vox;

namespace Dargon.Courier.StateReplicationTier.Primaries {
   public class StatePublisher<TState, TSnapshot, TDelta, TOperations> 
      where TState : class, IState 
      where TSnapshot : IStateSnapshot 
      where TDelta : class, IStateDelta
      where TOperations : IStateDeltaOperations<TState, TSnapshot, TDelta> {
      private const object kCreateCatchupSnapshotDtoMarker = null;
      private readonly Publisher publisher;
      private readonly TOperations ops;
      private readonly Guid topicId;
      private readonly Channel<object> outboundChannel;
      private readonly AsyncLatch shutdownLatch = new AsyncLatch();
      private readonly AsyncLock sync = new AsyncLock();
      private IStateView<TState> stateView;
      private Task publishLoopTask;
      private int snapshotEpoch = 0;
      private int deltaSeq = 0;

      public StatePublisher(Publisher publisher, TOperations ops, Guid topicId) {
         this.publisher = publisher;
         this.ops = ops;
         this.topicId = topicId;
         this.outboundChannel = ChannelFactory.Blocking<object>();
      }

      public async Task InitializeAsync(IStateView<TState> stateView, StateLock stateLock) {
         this.stateView = stateView;
         
         await publisher.CreateLocalTopicAsync(topicId);

         using (await stateLock.CreateReaderGuardAsync()) {
            var captureSnapshot = ops.CaptureSnapshot(stateView.State);
            outboundChannel.WriteAsync(captureSnapshot).Forget(); // immediately queues (await would be for dequeue)
         }

         this.publishLoopTask = RunPublishLoopAsync();
      }

      public async Task ShutdownAsync() {
         shutdownLatch.SetOrThrow();

         await publishLoopTask.AssertIsNotNull();
         publishLoopTask = null;
      }

      private async Task RunPublishLoopAsync() {
         while (!shutdownLatch.IsSignalled) {
            var o = await outboundChannel.ReadAsync();

            if (o == kCreateCatchupSnapshotDtoMarker) {
               continue;
            }

            using var mut = await sync.LockAsync();
            if (o is TSnapshot) {
               snapshotEpoch++;
               deltaSeq = 0;
               await publisher.PublishToLocalTopicAsync(topicId, new StateUpdateDto {
                  IsSnapshot = true,
                  IsOutOfBand = false,
                  SnapshotEpoch = snapshotEpoch,
                  DeltaSeq = deltaSeq,
                  Payload = o,
               });
            } else {
               deltaSeq++;
               await publisher.PublishToLocalTopicAsync(topicId, new StateUpdateDto {
                  IsSnapshot = false,
                  IsOutOfBand = false,
                  SnapshotEpoch = snapshotEpoch,
                  DeltaSeq = deltaSeq,
                  Payload = o,
               });
            }
         }
      }

      /// <summary>
      /// Immediately writes a snapshot to the publish queue.
      /// </summary>
      /// <returns>A task which contains a task that is completed when the publish event is dequeued but not yet processed</returns>
      public async Task<Task> QueueSnapshotPublishAsync(TSnapshot snapshot) {
         using var mut = await sync.LockAsync();
         return outboundChannel.WriteAsync(snapshot); // queues but does not wait for dequeue
      }

      /// <summary>
      /// Immediately writes a delta to the publish queue
      /// </summary>
      /// <returns>A task which contains a task that is completed when the publish event is dequeued but not yet processed</returns>
      public async Task<Task> QueueDeltaPublishAsync(TDelta delta) {
         using var mut = await sync.LockAsync();
         return outboundChannel.WriteAsync(delta.AssertIsNotNull()); // queues but does not wait for dequeue
      }

      /// <summary>
      /// Creates a <seealso cref="StateUpdateDto"/> with the given up-to-date snapshot
      /// and timestamped with the current snapshot epoch and delta seqnum.
      ///
      /// This dto can be used to get other clients up-to-date.
      ///
      /// This must only be invoked when:
      /// * The state is locked (either with an exclusive write or for reading)
      /// * We are inside that lock
      /// </summary>
      public async Task<StateUpdateDto> CreateOutOfBandCatchupSnapshotUpdateAsync(TSnapshot upToDateSnapshot) {
         // queue a null to the outbound channel and wait for it to be dequeued, at which point
         // we know we've processed all outbound delta/snapshot writes and snapshotEpoch/deltaSeq
         // have caught up.
         //
         // since this method should only be called when we have exclusive access to state or it can
         // only be read to, we know there will be no deltas applied during this call; state cannot change.
         await outboundChannel.WriteAsync(kCreateCatchupSnapshotDtoMarker);

         // lock sync for barrier to access deltaseq/snapshotepoch.
         using var mut = await sync.LockAsync();

         return new StateUpdateDto {
            IsSnapshot = true,
            IsOutOfBand = true,
            SnapshotEpoch = snapshotEpoch,
            DeltaSeq = deltaSeq,
            Payload = upToDateSnapshot,
         };
      }
   }

   public class StateLock {
      public readonly AsyncReaderWriterLock Lock = new AsyncReaderWriterLock();
      public Task<AsyncReaderWriterLock.Guard> CreateReaderGuardAsync() => Lock.ReaderLockAsync();
      public Task<AsyncReaderWriterLock.Guard> CreateWriterGuardAsync() => Lock.WriterLockAsync();
   }
}