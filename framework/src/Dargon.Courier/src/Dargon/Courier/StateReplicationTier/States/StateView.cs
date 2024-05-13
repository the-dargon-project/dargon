using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Courier.StateReplicationTier.Primaries;
using Dargon.Courier.StateReplicationTier.Utils;

namespace Dargon.Courier.StateReplicationTier.States;

/// <summary>
/// The simplest of state views:
/// Supports applying deltas
/// Supports loading snapshots
/// Tracks local and replication version
/// </summary>
/// <typeparam name="TState"></typeparam>
/// <typeparam name="TSnapshot"></typeparam>
/// <typeparam name="TDelta"></typeparam>
/// <typeparam name="TOperations"></typeparam>
public class StateView<TState, TSnapshot, TDelta, TOperations> : IStateView<TState, TSnapshot, TDelta>
   where TState : class, IState
   where TSnapshot : IStateSnapshot
   where TDelta : class, IStateDelta
   where TOperations : IStateDeltaOperations<TState, TSnapshot, TDelta> {

   private readonly TState state;
   private readonly TOperations ops;
   private readonly AsyncReaderWriterLock arwl;
   private readonly Func<TState> getState;
   private int localVersion = 0;
   private ReplicationVersion replicationVersion = new();

   public StateView(TState state, TOperations ops) {
      this.state = state;
      this.ops = ops;
      arwl = new AsyncReaderWriterLock();
      getState = () => this.state;
   }

   public int Version => localVersion;
   public ReplicationVersion ReplicationVersion => replicationVersion;
   public TState State => state;
   public bool IsReady => true;
   public event StateViewDeltaAppliedEvent<TDelta> DeltaApplied;
   public event StateViewSnapshotLoadedEvent<TSnapshot> SnapshotLoaded;

   public void ApplyDeltaOrThrow(TDelta delta) => TryApplyDelta(delta).AssertIsTrue();

   public bool TryApplyDelta(TDelta delta, ReplicationVersion? newVersionOpt = null) {
      using var _ = arwl.CreateWriterGuard();
      return TryApplyDeltaUnderLock(delta, newVersionOpt);
   }

   public async Task ApplyDeltaOrThrowAsync(TDelta delta) {
      var res = await TryApplyDeltaAsync(delta);
      res.AssertIsTrue();
   }

   public async Task<bool> TryApplyDeltaAsync(TDelta delta) {
      await using var _ = await arwl.CreateWriterGuardAsync();
      return TryApplyDeltaUnderLock(delta);
   }

   private bool TryApplyDeltaUnderLock(TDelta delta, ReplicationVersion? newVersionOpt = null) {
      if (newVersionOpt is { } nv) {
         replicationVersion.Epoch.AssertEquals(nv.Epoch);
         replicationVersion.Seq.AssertEquals(nv.Seq - 1);
      }

      var success = ops.TryApplyDelta(state, delta);
      if (success) {
         localVersion++;
         replicationVersion.Seq++;
         DeltaApplied?.Invoke(new(delta, replicationVersion, localVersion));
      }
      return success;
   }

   public void LoadSnapshot(TSnapshot snapshot, ReplicationVersion? newVersionOpt = null) {
      using var _ = arwl.CreateWriterGuard();
      LoadSnapshotUnderLock(snapshot, newVersionOpt);
   }

   private void ApplyReplicationVersionBump(ReplicationVersion? newVersionOpt = null) {
      if (newVersionOpt is { } newVersion) {
         (replicationVersion <= newVersion).AssertIsTrue();
         replicationVersion = newVersion;
      } else {
         replicationVersion.Epoch++;
         replicationVersion.Seq = 0;
      }
      localVersion++;
   }

   public void Copy(TState other, ReplicationVersion? newVersionOpt = null) {
      using var _ = arwl.CreateWriterGuard();
      ApplyReplicationVersionBump(newVersionOpt);
      ops.Copy(other, state);
   }

   public async Task LoadSnapshotAsync(TSnapshot snapshot) {
      await using var _ = await arwl.CreateWriterGuardAsync();
      LoadSnapshotUnderLock(snapshot, null);
   }

   private void LoadSnapshotUnderLock(TSnapshot snapshot, ReplicationVersion? newVersionOpt) {
      ApplyReplicationVersionBump(newVersionOpt);
      ops.LoadSnapshot(state, snapshot);
      SnapshotLoaded?.Invoke(new(snapshot, replicationVersion, localVersion));
   }

   public SnapshotInfo<TSnapshot> CaptureSnapshotWithInfo() {
      using var _ = arwl.CreateReaderGuard();
      return CaptureSnapshotWithInfoUnderLock();
   }

   public async Task<SnapshotInfo<TSnapshot>> CaptureSnapshotWithInfoAsync() {
      await using var _ = await arwl.CreateReaderGuardAsync();
      return CaptureSnapshotWithInfoUnderLock();
   }

   private SnapshotInfo<TSnapshot> CaptureSnapshotWithInfoUnderLock() {
      var snapshot = ops.CaptureSnapshot(state);
      return new(snapshot, replicationVersion, localVersion);
   }

   public SyncStateGuard<TState> LockStateForRead() => new(arwl.CreateReaderGuard(), getState);
   public SyncStateGuard<TState> LockStateForWrite() => new(arwl.CreateWriterGuard(), getState);
   public Task<AsyncStateGuard<TState>> LockStateForReadAsync() => WrapAsyncGuard(arwl.CreateReaderGuardAsync());
   public Task<AsyncStateGuard<TState>> LockStateForWriteAsync() => WrapAsyncGuard(arwl.CreateWriterGuardAsync());
   private async Task<AsyncStateGuard<TState>> WrapAsyncGuard(Task<AsyncReaderWriterLock.AsyncGuard> t) => new(await t, getState);
}