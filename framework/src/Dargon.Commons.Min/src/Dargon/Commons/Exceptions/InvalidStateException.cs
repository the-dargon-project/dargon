using System;

namespace Dargon.Commons.Exceptions {
   public class InvalidStateException : Exception {
      public InvalidStateException() : base() { }
      public InvalidStateException(string message) : base(message) { }
   }
}
