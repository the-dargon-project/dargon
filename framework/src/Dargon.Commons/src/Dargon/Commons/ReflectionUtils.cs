using System.Runtime.CompilerServices;

namespace Dargon.Commons {
   public static partial class ReflectionUtils {
      public static int GetStructSize<T>() where T : unmanaged => Unsafe.SizeOf<T>();
   }
}