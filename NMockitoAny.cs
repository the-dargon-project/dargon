using System;

namespace ItzWarty.Test
{
   public class NMockitoAny : INMockitoSmartParameter
   {
      private readonly Type type;
      public NMockitoAny(Type type) { this.type = type; }
      public bool Test(object value) { return type.IsAssignableFrom(value == null ? typeof(object) : value.GetType()); }
   }
}