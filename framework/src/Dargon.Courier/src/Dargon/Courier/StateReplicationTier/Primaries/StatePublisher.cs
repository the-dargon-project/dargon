using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.AsyncAwait;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Channels;
using Dargon.Courier.PubSubTier.Publishers;
using Dargon.Courier.StateReplicationTier.States;
using Dargon.Courier.StateReplicationTier.Vox;

namespace Dargon.Courier.StateReplicationTier.Primaries {
   public class StatePublisher<TState, TSnapshot, TDelta> 
      where TState : class, IState 
      where TSnapshot : IStateSnapshot 
      where TDelta : class, IStateDelta {
      private const object kCreateCatchupSnapshotDtoMarker = null;
      private readonly CourierSynchronizationContexts synchronizationContexts;
      private readonly Publisher publisher;
      private readonly Guid topicId;
      private readonly Channel<(object, ReplicationVersion, int)> outboundChannel;
      private readonly AsyncLatch shutdownLatch = new AsyncLatch();
      private IStateView<TState, TSnapshot, TDelta> stateView;
      private Task publishLoopTask;

      public StatePublisher(CourierSynchronizationContexts synchronizationContexts, Publisher publisher, Guid topicId) {
         this.synchronizationContexts = synchronizationContexts;
         this.publisher = publisher;
         this.topicId = topicId;
         this.outboundChannel = ChannelFactory.Blocking<(object, ReplicationVersion, int)>();
      }

      public async Task InitializeAsync(IStateView<TState, TSnapshot, TDelta> stateView) {
         this.stateView = stateView;

         stateView.DeltaApplied += HandleDeltaApplied;
         stateView.SnapshotLoaded += HandleSnapshotLoaded;

         var capture = await stateView.CaptureSnapshotWithInfoAsync();
         HandleSnapshotLoaded(new(capture.Snapshot, capture.ReplicationVersion, capture.LocalVersion));

         await publisher.CreateLocalTopicAsync(topicId);
         this.publishLoopTask = RunPublishLoopAsync().Forgettable();
      }

      public async Task ShutdownAsync() {
         shutdownLatch.SetOrThrow();

         await publishLoopTask.AssertIsNotNull();
         publishLoopTask = null;
      }

      private void HandleDeltaApplied(in StateViewDeltaAppliedEventArgs<TDelta> e) {
         var what = (e.Delta, e.ReplicationVersion, e.LocalVersion);
         outboundChannel.WriteAsync(what).Forget();
      }

      private void HandleSnapshotLoaded(in StateViewSnapshotLoadedEventArgs<TSnapshot> e) {
         var what = (e.Snapshot, e.ReplicationVersion, e.LocalVersion);
         outboundChannel.WriteAsync(what).Forget(); // immediately queues (await would be for dequeue)
      }

      private async Task RunPublishLoopAsync() {
         await synchronizationContexts.CourierDefault__.YieldToAsync();

         while (!shutdownLatch.IsSignalled) {
            var (o, ver, localVersion) = await outboundChannel.ReadAsync();

            if (o == kCreateCatchupSnapshotDtoMarker) {
               continue;
            }

            await publisher.PublishToLocalTopicAsync(topicId, new StateUpdateDto {
               IsSnapshot = o is TSnapshot,
               IsOutOfBand = false,
               Version = ver,
               Payload = o,
               VanityTotalSeq = localVersion,
            });
         }
      }

      public async Task<StateUpdateDto> CreateOutOfBandCatchupSnapshotUpdateAsync() {
         await synchronizationContexts.CourierDefault__.YieldToAsync();

         // queue a null to the outbound channel and wait for it to be dequeued, at which point
         // we know we've processed all outbound delta/snapshot writes and snapshotEpoch/deltaSeq
         // have caught up.
         //
         // since this method should only be called when we have exclusive access to state or it can
         // only be read to, we know there will be no deltas applied during this call; state cannot change.
         await outboundChannel.WriteAsync((kCreateCatchupSnapshotDtoMarker, default, default));

         var capture = await stateView.CaptureSnapshotWithInfoAsync();

         return new StateUpdateDto {
            IsSnapshot = true,
            IsOutOfBand = true,
            Version = capture.ReplicationVersion,
            Payload = capture.Snapshot,
            VanityTotalSeq = capture.LocalVersion,
         };
      }
   }
}