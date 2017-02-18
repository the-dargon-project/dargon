using System;

namespace NMockito.Attributes {
   [AttributeUsage(AttributeTargets.Field)]
   public class MockAttribute : Attribute {
      public bool IsTracked { get; set; } = true;
   }
}
