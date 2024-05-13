using System;
using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.StateReplicationTier.Primaries;

[VoxType((int)CourierVoxTypeIds.ReplicationVersion)]
public partial struct ReplicationVersion : IComparable<ReplicationVersion> {
   public int Epoch; // Updated on snapshot load
   public int Seq; // Updated on delta apply

   public override int GetHashCode() {
      return HashCode.Combine(Epoch, Seq);
   }
   
   public static bool operator ==(ReplicationVersion a, ReplicationVersion b) => a.Epoch == b.Epoch && a.Seq == b.Seq;
   public static bool operator !=(ReplicationVersion a, ReplicationVersion b) => !(a == b);
   public static bool operator <(ReplicationVersion a, ReplicationVersion b) => a.CompareTo(b) < 0;
   public static bool operator >(ReplicationVersion a, ReplicationVersion b) => a.CompareTo(b) > 0;
   public static bool operator <=(ReplicationVersion a, ReplicationVersion b) => a.CompareTo(b) <= 0;
   public static bool operator >=(ReplicationVersion a, ReplicationVersion b) => a.CompareTo(b) >= 0;

   public bool Equals(ReplicationVersion other) => this == other;
   public override bool Equals(object obj) => obj is ReplicationVersion other && Equals(other);

   public int CompareTo(ReplicationVersion other) {
      var epochComparison = Epoch.CompareTo(other.Epoch);
      if (epochComparison != 0) return epochComparison;
      return Seq.CompareTo(other.Seq);
   }

   public override string ToString() => $"[{Epoch}:{Seq}]";
}