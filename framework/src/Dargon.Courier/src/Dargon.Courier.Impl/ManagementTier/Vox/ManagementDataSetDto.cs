using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Vox;

namespace Dargon.Courier.ManagementTier.Vox {
   [AutoSerializable]
   public class ManagementDataSetDto<T> {
      public DataPoint<T>[] DataPoints { get; set; }
   }
}
