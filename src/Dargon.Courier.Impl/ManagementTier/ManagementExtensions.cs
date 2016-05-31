using Dargon.Commons.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Courier.ManagementTier
{
    public static class ManagementExtensions
    {

      public static void RegisterService(this MobOperations mobOperations, object service) {
         Guid serviceId;
         if (!service.GetType().TryGetInterfaceGuid(out serviceId)) {
            throw new InvalidOperationException($"Mob of type {service.GetType().FullName} does not have default service id.");
         }
         mobOperations.RegisterService(serviceId, service);
      }

      public static void RegisterService(this MobOperations mobOperations, Guid mobId, object mobInstance) {
         mobOperations.RegisterService(mobId, mobInstance, mobInstance.GetType().FullName);
      }
   }
}
