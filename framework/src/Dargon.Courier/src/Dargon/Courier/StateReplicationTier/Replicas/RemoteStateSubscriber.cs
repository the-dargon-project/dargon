using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Channels;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.PubSubTier.Publishers;
using Dargon.Courier.PubSubTier.Subscribers;
using Dargon.Courier.PubSubTier.Vox;
using Dargon.Courier.StateReplicationTier.States;
using Dargon.Courier.StateReplicationTier.Vox;

namespace Dargon.Courier.StateReplicationTier.Replicas {
   public class RemoteStateSubscriber<TState, TSnapshot, TDelta> where TState : IState where TSnapshot : IStateSnapshot where TDelta : class, IStateDelta {
      private readonly Subscriber subscriber;
      private readonly PeerContext remote;
      private readonly Guid topicId;
      private readonly StateUpdateProcessor<TState, TSnapshot, TDelta> updateProcessor;

      public RemoteStateSubscriber(Subscriber subscriber, PeerContext remote, Guid topicId, StateUpdateProcessor<TState, TSnapshot, TDelta> updateProcessor) {
         this.subscriber = subscriber;
         this.remote = remote;
         this.topicId = topicId;
         this.updateProcessor = updateProcessor;
      }

      public async Task InitializeAsync() {
         await subscriber.SubscribeToRemoteTopicAsync(topicId, remote, HandleRemotePublishAsync);
      }

      private Task HandleRemotePublishAsync(PubSubNotification notification) {
         var stateUpdate = (StateUpdateDto)notification.Payload;
         updateProcessor.Enqueue(stateUpdate);
         return Task.CompletedTask;
      }
   }

   public class StateUpdateProcessor<TState, TSnapshot, TDelta> where TState : IState where TSnapshot : IStateSnapshot where TDelta : class, IStateDelta {
      private readonly TState state;
      private readonly IStateDeltaOperations<TState, TSnapshot, TDelta> ops;

      private readonly ConcurrentQueue<StateUpdateDto> inboundUpdates = new();
      private readonly SortedList<int, SortedList<int, StateUpdateDto>> unprocessedUpdatesPerEpoch = new();
      private int currentEpoch = -1;
      private int lastAppliedSeqInEpoch = -1;
      private int version = -1;

      public StateUpdateProcessor(TState state, IStateDeltaOperations<TState, TSnapshot, TDelta> ops) {
         this.state = state;
         this.ops = ops;
      }

      public int Version => version;

      public void Enqueue(StateUpdateDto stateUpdate) {
         inboundUpdates.Enqueue(stateUpdate);
      }

      public bool HasInboundUpdates => inboundUpdates.Count > 0;

      public void IngestInboundUpdates() {
         while (inboundUpdates.Count > 0) {
            var update = inboundUpdates.DequeueOrThrow();
            if (update.SnapshotEpoch < currentEpoch) continue; // throw away
            if (!unprocessedUpdatesPerEpoch.TryGetValue(update.SnapshotEpoch, out var seqToUpdate)) {
               seqToUpdate = unprocessedUpdatesPerEpoch[update.SnapshotEpoch] = new();
            }

            seqToUpdate.Add(update.DeltaSeq, update);
         }

         AdvanceToNextEpochIfPossible();

         if (currentEpoch == -1) {
            // no epoch started yet
            return;
         }

         var unprocessedUpdatesOfCurrentEpoch = unprocessedUpdatesPerEpoch[currentEpoch];
         while (unprocessedUpdatesOfCurrentEpoch.Remove(lastAppliedSeqInEpoch + 1, out var updateToApply)) {
            if (updateToApply.IsSnapshot) {
               ops.LoadSnapshot(state, (TSnapshot)updateToApply.Payload);
            } else {
               ops.TryApplyDelta(state, (TDelta)updateToApply.Payload).AssertIsTrue();
            }

            version++;
         }
      }

      private void AdvanceToNextEpochIfPossible() {
         foreach (var epochId in unprocessedUpdatesPerEpoch.Keys) {
            var unprocessedUpdatesOfEpoch = unprocessedUpdatesPerEpoch[epochId];
            if (unprocessedUpdatesOfEpoch.ContainsKey(StateUpdateDto.kSnapshotDeltaSeq)) {
               currentEpoch = epochId;
               lastAppliedSeqInEpoch = -1;

               while (true) {
                  var otherEpochId = unprocessedUpdatesPerEpoch.Keys[0];
                  if (otherEpochId < currentEpoch) {
                     unprocessedUpdatesPerEpoch.Remove(otherEpochId);
                  } else {
                     return;
                  }
               }
            }
         }
      }
   }
}