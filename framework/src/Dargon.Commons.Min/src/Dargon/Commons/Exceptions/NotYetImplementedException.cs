using System;

namespace Dargon.Commons.Exceptions {
   public class NotYetImplementedException : Exception {
      public NotYetImplementedException() : this("Not yet (but can eventually be!) implemented.") { }
      public NotYetImplementedException(string message) : base(message) { }
   }
}