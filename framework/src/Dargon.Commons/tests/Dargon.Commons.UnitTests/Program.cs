using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Commons.Pooling;

namespace Dargon.Commons {
   public static class Program {
      public static void Main() {
         new AsyncLocalBufferManagerTests().Run().Wait();
      }
   }
}
