using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libtestutil
{
    public static class MoqHelper
    {
       public static void InitializeMocks(object test)
       {
          new MockInitializationProcessor().Process(test);
       }
    }
}
