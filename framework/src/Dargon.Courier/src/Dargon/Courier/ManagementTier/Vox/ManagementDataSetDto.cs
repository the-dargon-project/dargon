using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.ManagementTier.Vox {
   [VoxType((int)CourierVoxTypeIds.ManagementDataSetDto)]
   public partial class ManagementDataSetDto<T> {
      public DataPoint<T>[] DataPoints { get; set; }
   }
}
