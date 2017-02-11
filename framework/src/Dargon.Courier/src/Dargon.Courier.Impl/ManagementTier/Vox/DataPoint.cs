using Dargon.Vox;
using System;

namespace Dargon.Courier.ManagementTier.Vox {
   [AutoSerializable]
   public class DataPoint<T> {
      public DateTime Time { get; set; }
      public T Value { get; set; }
   }
}