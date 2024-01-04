using System;

namespace Dargon.Commons.Exceptions {
   public class BadInputException : Exception {
      public BadInputException() : base() { }
      public BadInputException(string message) : base(message) { }
   }
}
