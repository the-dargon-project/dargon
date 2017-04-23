using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Courier {
   public static class CourierFacadeExtensions {
      public static Task BroadcastAsync<T>(this CourierFacade facade, T payload) {
         return facade.Messenger.BroadcastAsync(payload);
      }

      public static Task SendUnreliableAsync<T>(this CourierFacade facade, T payload, Guid destination) {
         return facade.Messenger.SendUnreliableAsync(payload, destination);
      }

      public static Task SendReliableAsync<T>(this CourierFacade facade, T payload, Guid destination) {
         return facade.Messenger.SendReliableAsync(payload, destination);
      }
   }
}
