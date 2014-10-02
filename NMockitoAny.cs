using System;

namespace NMockito
{
   public class NMockitoAny<T> : INMockitoSmartParameter
   {
      private readonly Func<T, bool> test;
      public NMockitoAny(Func<T, bool> test) { this.test = test ?? (t => true); }
      public bool Test(object value) { return typeof(T).IsAssignableFrom(value == null ? typeof(object) : value.GetType()) && test((T)value); }
   }
}