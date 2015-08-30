using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Services;
using ItzWarty.Collections;
using NMockito;

namespace Dargon.Ryu.Impl.Tests {
   public class RemoteServiceContainerImplTests : NMockitoInstance {
      [Mock] private readonly IServiceClient serviceClient;
      [Mock] private readonly IConcurrentDictionary<Type, object> servicesByType;

      private readonly RemoteServiceContainerImpl testObj;

      public RemoteServiceContainerImplTests() {
         this.testObj = new RemoteServiceContainerImpl(serviceClient, servicesByType);
      }
   }
}
