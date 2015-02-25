using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.PortableObjects;
using Xunit;

namespace Dargon.Courier.Impl.Tests {
   public class PofSerializationTests {
      private readonly IPofContext parentContext;
      private readonly IPofSerializer parentSerializer;
      private readonly DargonCourierImplPofContext courierImplContext;
      private readonly CourierEndpointImpl localCourierEndpoint;

      public PofSerializationTests() {
         var parentContextImpl = new PofContext();
         parentContext = parentContextImpl;
         parentSerializer = new PofSerializer(parentContext);
         parentContextImpl.MergeContext(new DargonCourierImplPofContext(1000, parentSerializer));
         localCourierEndpoint = new CourierEndpointImpl(parentSerializer, Guid.NewGuid());
         localCourierEndpoint.SetProperty(CourierEndpointPropertyKeys.Name, "Test");
      }

      [Fact]
      public void CourierEndpointImplSerializableTest() {
         PofTestUtilities.CheckConfiguration(parentContext, localCourierEndpoint);
      }
   }
}
