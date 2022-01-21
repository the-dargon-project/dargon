using System;

namespace Dargon.Courier.PubSubTier.Vox {
   public class PubSubNotification {
      public Guid Topic;
      public uint Seq;
      public object Payload;
   }
}