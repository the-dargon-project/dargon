using System;

namespace Dargon.Ryu {
   public class NoConstructorsFoundException : Exception {
      public NoConstructorsFoundException(Type type) : base(GetMessage(type)) { }

      private static string GetMessage(Type type) {
         return $"Could not find any constructors for type {type.FullName}!";
      }
   }
}