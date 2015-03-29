using System;

namespace Dargon.Courier.Messaging {
   public interface NetworkBroadcaster {
      void SendCourierPacket<TPayload>(TPayload payload);
   }
}
