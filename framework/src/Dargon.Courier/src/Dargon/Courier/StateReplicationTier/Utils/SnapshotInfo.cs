using Dargon.Courier.StateReplicationTier.Primaries;

namespace Dargon.Courier.StateReplicationTier.Utils;

public record struct SnapshotInfo<TSnapshot>(TSnapshot Snapshot, ReplicationVersion ReplicationVersion, int LocalVersion);