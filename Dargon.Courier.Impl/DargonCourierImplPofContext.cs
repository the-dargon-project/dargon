using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.PortableObjects;

namespace Dargon.Courier {
   public class DargonCourierImplPofContext : PofContext {
      public DargonCourierImplPofContext(int basePofId, IPofSerializer parentSerializer) {
         RegisterPortableObjectType(basePofId + 0, () => new CourierEndpointImpl(parentSerializer));
      }
   }
}
