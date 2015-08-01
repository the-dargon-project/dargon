using System;

namespace Dargon.Ryu {
   public class ImplementationNotDefinedException : Exception {
      public ImplementationNotDefinedException(Type type) : base(GetMessage(type)) { }

      private static string GetMessage(Type type) {
         return $"Implementation for type {type.FullName} not found!";
      }
   }
}