using Dargon.Commons.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Courier.ManagementTier {
   public static class ManagementExtensions {

      public static void RegisterMob(this MobOperations mobOperations, object mobInstance) {
         Guid mobId;
         if (!mobInstance.GetType().TryGetInterfaceGuid(out mobId)) {
            throw new InvalidOperationException($"Mob of type {mobInstance.GetType().FullName} does not have default service id.");
         }

         RegisterMob(mobOperations, mobId, mobInstance);
      }

      public static void RegisterMob(this MobOperations mobOperations, Guid mobId, object mobInstance) {
         mobOperations.RegisterMob(mobId, mobInstance, mobInstance.GetType().FullName);
      }
   }
}
