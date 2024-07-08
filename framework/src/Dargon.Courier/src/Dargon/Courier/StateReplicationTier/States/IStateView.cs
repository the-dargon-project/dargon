using System;
using System.Threading.Tasks;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.VersionCounters;
using Dargon.Courier.StateReplicationTier.Primaries;
using Dargon.Courier.StateReplicationTier.Utils;

namespace Dargon.Courier.StateReplicationTier.States;

public readonly record struct StateViewDeltaAppliedEventArgs<TDelta>(TDelta Delta, ReplicationVersion ReplicationVersion, int LocalVersion);
public delegate void StateViewDeltaAppliedEvent<TDelta>(in StateViewDeltaAppliedEventArgs<TDelta> e);

public readonly record struct StateViewSnapshotLoadedEventArgs<TSnapshot>(TSnapshot Snapshot, ReplicationVersion ReplicationVersion, int LocalVersion);
public delegate void StateViewSnapshotLoadedEvent<TSnapshot>(in StateViewSnapshotLoadedEventArgs<TSnapshot> e);

public interface IStateView : IVersionSource {
   bool IsReady { get; }
}

public interface IStateView<TState> : IStateView where TState : class, IState { }

public interface IStateView<TState, TSnapshot, TDelta> : IStateView<TState>
   where TState : class, IState
   where TSnapshot : IStateSnapshot
   where TDelta : class, IStateDelta {
   /// <summary>
   /// Always invoked prior to any corresponding Updated events.
   /// Invoked synchronously under state lock.
   /// </summary>
   event StateViewDeltaAppliedEvent<TDelta> DeltaApplied;

   /// <summary>
   /// Invoked synchronously under state lock.
   /// </summary>
   event StateViewSnapshotLoadedEvent<TSnapshot> SnapshotLoaded;

   // Primaries
   SnapshotInfo<TSnapshot> CaptureSnapshotWithInfo();
   Task<SnapshotInfo<TSnapshot>> CaptureSnapshotWithInfoAsync();

   // Replicas
   void LoadSnapshot(TSnapshot snapshot, ReplicationVersion? version = null);
   bool TryApplyDelta(TDelta delta, ReplicationVersion? version = null);
   void Copy(TState other, ReplicationVersion? version = null);

   // Prediction & Processing
   SyncStateGuard<TState> LockStateForRead();
   SyncStateGuard<TState> LockStateForWrite();
   Task<AsyncStateGuard<TState>> LockStateForReadAsync();
   Task<AsyncStateGuard<TState>> LockStateForWriteAsync();
}


public struct SyncStateGuard<TState>(AsyncReaderWriterLock.SyncGuard inner, Func<TState> getState) : IDisposable {
   public TState State => getState();

   public void Dispose() {
      inner.Dispose();
   }
}

public struct AsyncStateGuard<TState>(AsyncReaderWriterLock.AsyncGuard inner, Func<TState> getState) : IAsyncDisposable {
   public TState State => getState();

   // must not await the inner task, as that'd scope alsReaderDepth
   public ValueTask DisposeAsync() => inner.DisposeAsync();
}