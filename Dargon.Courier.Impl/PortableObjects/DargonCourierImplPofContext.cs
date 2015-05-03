using Dargon.PortableObjects;

namespace Dargon.Courier.PortableObjects {
   public class DargonCourierImplPofContext : PofContext {
      public const int kBasePofId = 4000;
      public DargonCourierImplPofContext() {
         RegisterPortableObjectType(kBasePofId + 0, typeof(CourierAnnounceV1));
         RegisterPortableObjectType(kBasePofId + 1, typeof(CourierMessageV1));
         RegisterPortableObjectType(kBasePofId + 2, typeof(CourierMessageAcknowledgeV1));
      }
   }
}
