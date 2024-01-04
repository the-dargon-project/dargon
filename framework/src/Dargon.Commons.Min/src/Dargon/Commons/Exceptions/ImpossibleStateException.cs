using System;

namespace Dargon.Commons.Exceptions {
   public class ImpossibleStateException : Exception {
      public ImpossibleStateException() : base() { }
      public ImpossibleStateException(string message) : base(message) { }
   }
}