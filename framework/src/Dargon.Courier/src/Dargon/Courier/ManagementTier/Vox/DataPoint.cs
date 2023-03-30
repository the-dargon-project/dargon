using Dargon.Courier.Vox;
using Dargon.Vox2;
using System;

namespace Dargon.Courier.ManagementTier.Vox {
   [VoxType((int)CourierVoxTypeIds.DataPoint)]
   public partial class DataPoint<T> {
      public DateTime Time { get; set; }
      public T Value { get; set; }
   }
}