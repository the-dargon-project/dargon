using System;

namespace NMockito.Assertions {
   public class NothingThrownException : Exception {
      public NothingThrownException(Type expected) : base($"No exception thrown. Expected to encounter ${expected.FullName}.") { }
      public NothingThrownException(Type outerExpected, Type innerExpected) : base($"No exception thrown. Expected to encounter outer exception {outerExpected.FullName} with inner exception {innerExpected.FullName}.") { }
   }
}