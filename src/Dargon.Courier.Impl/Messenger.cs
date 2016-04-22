using System;
using Dargon.Courier.Vox;

namespace Dargon.Courier {
   public class Messenger {
      public void Broadcast<T>(T payload) {
         Helper(MessageDto.Create(payload, Guid.Empty, MessageFlags.None));
      }

      public void Send<T>(T payload, Guid destination) {
         Helper(MessageDto.Create(payload, destination, MessageFlags.None));
      }

      public void SendReliable<T>(T payload, Guid destination) {
         Helper(MessageDto.Create(payload, destination, MessageFlags.Reliable));
      }

      private void Helper(MessageDto message) {
      }
   }
}
