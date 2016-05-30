using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Courier.ServiceTier.Server;

namespace Dargon.Courier.ServiceTier {
   public static class ServiceTierExtensions {
      public static void RegisterService<TServiceInterface>(this LocalServiceRegistry localServiceRegistry, TServiceInterface implementation) {
         localServiceRegistry.RegisterService(typeof(TServiceInterface), implementation);
      }
   }
}
