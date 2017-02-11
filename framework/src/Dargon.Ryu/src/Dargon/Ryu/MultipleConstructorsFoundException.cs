using System;

namespace Dargon.Ryu {
   public class MultipleConstructorsFoundException : Exception {
      public MultipleConstructorsFoundException(Type type) : base(GetMessage(type)) { }

      private static string GetMessage(Type type) {
         return $"Multiple constructors encountered for type {type.FullName}. Consider leveraging {nameof(RyuConstructorAttribute)} on one constructor.";
      }
   }
}