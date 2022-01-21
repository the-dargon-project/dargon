using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Courier.ServiceTier.Server {
   public static class LocalServiceRegistryExtensions {
      public static void RegisterService<TServiceInterface>(this LocalServiceRegistry localServiceRegistry, TServiceInterface implementation) {
         localServiceRegistry.RegisterService(typeof(TServiceInterface), implementation);
      }

      public static void RegisterService<TServiceInterface>(this LocalServiceRegistry localServiceRegistry, Guid serviceGuid, TServiceInterface implementation) {
         localServiceRegistry.RegisterService(serviceGuid, typeof(TServiceInterface), implementation);
      }
   }
}
