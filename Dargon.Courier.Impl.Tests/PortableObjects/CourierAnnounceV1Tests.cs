using System;
using Dargon.Courier.PortableObjects;
using NMockito;
using Xunit;

namespace Dargon.Courier.Impl.PortableObjects {
   public class CourierAnnounceV1Tests : NMockitoInstance {
      private readonly Guid senderGuid = Guid.NewGuid();
      private readonly int propertiesHash = 0x12AE812F;
      private readonly byte[] propertiesData = new byte[] { 1, 2, 3, 4, 5 };

      private readonly CourierAnnounceV1 testObj;

      public CourierAnnounceV1Tests() {
         testObj = new CourierAnnounceV1(senderGuid, propertiesHash, propertiesData);
      }

      [Fact]
      public void PofSerializationTest() {
         PofTestUtilities.CheckConfiguration(new DargonCourierImplPofContext(0), testObj);
      }
   }
}
