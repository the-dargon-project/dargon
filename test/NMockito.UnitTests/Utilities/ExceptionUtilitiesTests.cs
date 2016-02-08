using System;
using Xunit;

namespace NMockito.Utilities {
   public class ExceptionUtilitiesTests {
      [Fact]
      public void Rethrow_Throws() {
         Assert.Throws<InvalidOperationException>(() => new InvalidOperationException().Rethrow());
         Assert.Throws<ArgumentException>(() => new ArgumentException().Rethrow());
      }
   }
}