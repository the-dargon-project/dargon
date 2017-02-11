using Dargon.Vox;

namespace Dargon.Courier.AuditingTier {
   [AutoSerializable]
   public class AggregateStatistics<T> {
      public T Sum { get; set; }
      public T Min { get; set; }
      public T Max { get; set; }
      public int Count { get; set; }
      public T Average { get; set; }
   }
}