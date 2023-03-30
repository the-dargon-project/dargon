using System;
using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.PubSubTier.Vox {
   [VoxType((int)CourierVoxTypeIds.PubSubNotification)]
   public partial class PubSubNotification {
      public Guid Topic;
      public uint Seq;
      [P] public object Payload;
   }
}