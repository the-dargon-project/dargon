using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItzWarty.Test
{
   public class NMockitoTimes
   {
      private readonly int value;
      public NMockitoTimes(int value) { this.value = value; }
      public int Value { get { return value; } }

      public override string ToString() { return "[Times (" + value + ")]"; }
   }
}
