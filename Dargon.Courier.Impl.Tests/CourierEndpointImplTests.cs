using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.PortableObjects;
using NMockito;

namespace Dargon.Courier.Impl.Tests {
   public class CourierEndpointImplTests : NMockitoInstance {
      [Mock] private readonly IPofSerializer pofSerializer = null;
      private readonly Guid identifier = Guid.NewGuid();
      private readonly CourierEndpointImpl testObj;

      public CourierEndpointImplTests() {
         testObj = new CourierEndpointImpl(pofSerializer, identifier);
      }

      public void Run() {

      }
   }
}
