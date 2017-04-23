using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Courier.ServiceTier.Server {
   public static class LocalServiceRegistryExtensions {
      public static void RegisterService<TServiceInterface>(this LocalServiceRegistry localServiceRegistry, object service) {
         localServiceRegistry.RegisterService(typeof(TServiceInterface), service);
      }
   }
}
