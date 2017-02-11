using System;

namespace Dargon.Courier {
   [AttributeUsage(AttributeTargets.Property)]
   public class ManagedPropertyAttribute : Attribute {
      public bool IsDataSource { get; set; } = false;
   }
}