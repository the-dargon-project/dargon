using System;
using Dargon.Courier.PeeringTier;

namespace Dargon.Courier.ServiceTier.Client {
   public class RemoveServiceInfo {
      public Type ServiceType { get; set; }
      public Guid ServiceId { get; set; }
      public PeerContext Peer { get; set; }
   }
}