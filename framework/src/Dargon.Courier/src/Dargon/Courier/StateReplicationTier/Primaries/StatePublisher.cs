using System;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Channels;
using Dargon.Courier.PubSubTier.Publishers;
using Dargon.Courier.StateReplicationTier.States;
using Dargon.Courier.StateReplicationTier.Vox;

namespace Dargon.Courier.StateReplicationTier.Primaries {
   public class StatePublisher<TState, TSnapshot, TDelta, TOperations> 
      where TState : class, IState 
      where TSnapshot : IStateSnapshot 
      where TDelta : class, IStateDelta
      where TOperations : IStateDeltaOperations<TState, TSnapshot, TDelta> {
      private readonly Publisher publisher;
      private readonly TOperations ops;
      private readonly Guid topicId;
      private readonly Channel<object> outboundChannel;
      private readonly AsyncLatch shutdownLatch = new AsyncLatch();
      private readonly AsyncLock sync = new AsyncLock();
      private IStateView<TState> stateView;
      private Task publishLoopTask;

      public StatePublisher(Publisher publisher, TOperations ops, Guid topicId) {
         this.publisher = publisher;
         this.ops = ops;
         this.topicId = topicId;
         this.outboundChannel = ChannelFactory.Blocking<object>();
      }

      public void Initialize(IStateView<TState> stateView) {
         this.stateView = stateView;
         outboundChannel.WriteAsync(ops.CaptureSnapshot(stateView.State)); // immediately queues (await would be for dequeue)
         this.publishLoopTask = RunPublishLoopAsync();
      }

      public async Task ShutdownAsync() {
         shutdownLatch.SetOrThrow();

         await publishLoopTask.AssertIsNotNull();
         publishLoopTask = null;
      }

      private async Task RunPublishLoopAsync() {
         await publisher.CreateLocalTopicAsync(topicId);

         int snapshotEpoch = 0;
         int deltaSeq = 0;
         while (!shutdownLatch.IsSignalled) {
            var o = await outboundChannel.ReadAsync();

            if (o is TSnapshot) {
               snapshotEpoch++;
               deltaSeq = 0;
               await publisher.PublishToLocalTopicAsync(topicId, new StateUpdateDto {
                  IsSnapshot = true,
                  SnapshotEpoch = snapshotEpoch,
                  DeltaSeq = deltaSeq,
                  Payload = o,
               });
            } else {
               deltaSeq++;
               await publisher.PublishToLocalTopicAsync(topicId, new StateUpdateDto {
                  IsSnapshot = false,
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
         using var publishLock = await sync.LockAsync();
         return outboundChannel.WriteAsync(snapshot); // queues but does not wait for dequeue
      }

      /// <summary>
      /// Immediately writes a delta to the publish queue
      /// </summary>
      /// <returns>A task which contains a task that is completed when the publish event is dequeued but not yet processed</returns>
      public async Task<Task> QueueDeltaPublishAsync(TDelta delta) {
         using var publishLock = await sync.LockAsync();
         return outboundChannel.WriteAsync(delta.AssertIsNotNull()); // queues but does not wait for dequeue
      }
   }
}