using System;
using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.PubSubTier.Vox {
   [VoxType((int)CourierVoxTypeIds.PubSubNotification)]
   public class PubSubNotification {
      public Guid Topic;
      public uint Seq;
      public object Payload;
   }
}