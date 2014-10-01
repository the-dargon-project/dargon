namespace ItzWarty.Test
{
   public static class NMockitoAnys
   {
      public static T CreateAny<T>() { return default(T); }
   }
}