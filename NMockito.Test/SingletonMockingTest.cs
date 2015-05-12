using Xunit;

namespace NMockito {
   public class SingletonMockingTest : NMockitoInstance {
      [Mock(StaticType = typeof(Util))] private readonly UtilInterface util = null;

      [Fact]
      public void Run() {
         When(util.GetCount()).ThenReturn(1);
         AssertEquals(1, Util.GetCount());
      }
   }

   public static class Util {
      private static readonly UtilInterface instance = new UtilImpl();

      public static int GetCount() => instance.GetCount();
   }

   public interface UtilInterface {
      int GetCount();
   }

   public class UtilImpl : UtilInterface {
      public int GetCount() {
         return 0;
      }
   }
}
