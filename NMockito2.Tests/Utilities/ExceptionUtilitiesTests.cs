using System;
using System.IO;
using Xunit;

namespace NMockito2.Utilities {
   public class ExceptionUtilitiesTests {
      [Fact]
      public void Rethrow_Throws() {
         Assert.Throws<InvalidOperationException>(() => new InvalidOperationException().Rethrow());
         Assert.Throws<ArgumentException>(() => new ArgumentException().Rethrow());
      }
   }
}