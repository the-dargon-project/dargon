using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Courier.StateReplicationTier.States;

namespace Dargon.Courier.StateReplicationTier.Filters {
   public struct StateFilterArg<TState, TDelta> where TState : class, IState where TDelta : class, IStateDelta {
      public TState Baseline { get; init; }
      public TState Replica { get; init; }
      public TDelta Delta { get; set; }
   }

   public interface IStateFilter<TState, TSnapshot, TDelta> : IStateView<TState>
      where TState : class, IState
      where TSnapshot : IStateSnapshot
      where TDelta : class, IStateDelta {
      void Reconcile(TState baseline, TState replica);
      void PreFilterDelta(ref StateFilterArg<TState, TDelta> args);
      void PostFilterDelta(ref StateFilterArg<TState, TDelta> args);
   }

   public interface IStateFilterPipeline { }

   public class StateFilterPipeline<TState, TSnapshot, TDelta> : IStateFilterPipeline
      where TState : class, IState
      where TSnapshot : IStateSnapshot
      where TDelta : class, IStateDelta {
      private readonly IStateView<TState, TSnapshot, TDelta> src;
      private readonly IStateView<TState, TSnapshot, TDelta> dst;
      private readonly IStateFilter<TState, TSnapshot, TDelta> filter;

      public StateFilterPipeline(
         IStateView<TState, TSnapshot, TDelta> src, 
         IStateView<TState, TSnapshot, TDelta> dst,
         IStateFilter<TState, TSnapshot, TDelta> filter) {
         this.src = src;
         this.dst = dst;
         this.filter = filter;
      }

      public void Initialize() {
         using var srcGuard = src.LockStateForRead();
         using var dstGuard = dst.LockStateForWrite();
         filter.Reconcile(srcGuard.State, dstGuard.State);

         src.DeltaApplied += HandleSourceDeltaApplied;
         src.SnapshotLoaded += HandleSourceSnapshotLoaded;
      }

      private void HandleSourceDeltaApplied(in StateViewDeltaAppliedEventArgs<TDelta> e) {
         using var srcGuard = src.LockStateForRead();
         using var dstGuard = dst.LockStateForWrite();
         var originalDelta = e.Delta;
         var args = new StateFilterArg<TState, TDelta> {
            Baseline = srcGuard.State,
            Replica = dstGuard.State,
            Delta = originalDelta,
         };
         filter.PreFilterDelta(ref args);
         if (args.Delta != null) {
            dst.TryApplyDelta(args.Delta).AssertIsTrue();
            filter.PostFilterDelta(ref args);
         }
      }

      private void HandleSourceSnapshotLoaded(in StateViewSnapshotLoadedEventArgs<TSnapshot> e) {
         Reconcile();
      }

      public void Reconcile() {
         using var srcGuard = src.LockStateForRead();
         using var dstGuard = dst.LockStateForWrite();
         filter.Reconcile(srcGuard.State, dstGuard.State);
      }
   }
}