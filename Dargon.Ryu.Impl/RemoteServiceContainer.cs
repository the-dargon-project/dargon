using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Services;
using ItzWarty.Collections;
using SCG = System.Collections.Generic;

namespace Dargon.Ryu {
   public interface RemoteServiceContainer {
      void AddService(Type serviceInterface);
      bool TryGetService(Type serviceType, out object service);
   }

   public class RemoteServiceContainerImpl : RemoteServiceContainer {
      private readonly IServiceClient serviceClient;
      private readonly IConcurrentDictionary<Type, object> servicesByInterface;

      public RemoteServiceContainerImpl(IServiceClient serviceClient, IConcurrentDictionary<Type, object> servicesByInterface) {
         this.serviceClient = serviceClient;
         this.servicesByInterface = servicesByInterface;
      }

      public void AddService(Type serviceInterface) {
         servicesByInterface.GetOrAdd(
            serviceInterface,
            add => { return null; }
         );
      }

      public bool TryGetService(Type serviceType, out object service) {
         throw new NotImplementedException();
      }
   }
}
