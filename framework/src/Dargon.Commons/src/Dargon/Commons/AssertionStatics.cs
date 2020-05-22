namespace Dargon.Commons {
   public static class AssertionStatics {
      public static bool AssertIsTrue(this bool val, string message = null) {
         Assert.IsTrue(val, message);
         return val;
      }

      public static bool AssertIsFalse(this bool val, string message = null) {
         Assert.IsTrue(val, message);
         return val;
      }
   }
}