using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.AuditingTier {
   [VoxType((int)CourierVoxTypeIds.AggregateStatistics)]
   public partial class AggregateStatistics<T> {
      public T Sum { get; set; }
      public T Min { get; set; }
      public T Max { get; set; }
      public int Count { get; set; }
      public T Average { get; set; }
   }
}