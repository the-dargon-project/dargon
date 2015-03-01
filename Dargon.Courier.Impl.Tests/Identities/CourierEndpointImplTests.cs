using Dargon.Courier.Identities;
using Dargon.PortableObjects;
using NMockito;
using System;

namespace Dargon.Courier.Impl.Identities {
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
