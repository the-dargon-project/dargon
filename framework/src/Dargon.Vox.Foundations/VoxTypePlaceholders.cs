using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Vox2 {
   public static class VoxTypePlaceholders {
      public static Type RuntimePolymorphicArray1Gtd = typeof(RuntimePolymorphicArray1<int>).GetGenericTypeDefinition();

      public class RuntimePolymorphicNull { }

      public class RuntimePolymorphicArray1<T> { }
   }
}
