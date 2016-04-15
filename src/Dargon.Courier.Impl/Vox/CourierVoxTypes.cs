using Dargon.Vox;

namespace Dargon.Courier.Vox {
   public class CourierVoxTypes : VoxTypes {
      public CourierVoxTypes() : base(0) {
         Register<MessageDto>(0);
      }
   }
}
