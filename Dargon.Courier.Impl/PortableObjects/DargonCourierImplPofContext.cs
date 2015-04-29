using Dargon.PortableObjects;

namespace Dargon.Courier.PortableObjects {
   public class DargonCourierImplPofContext : PofContext {
      public DargonCourierImplPofContext(int basePofId) {
         RegisterPortableObjectType(basePofId + 0, typeof(CourierAnnounceV1));
         RegisterPortableObjectType(basePofId + 1, typeof(CourierMessageV1));
         RegisterPortableObjectType(basePofId + 2, typeof(CourierMessageAcknowledgeV1));
      }
   }
}
