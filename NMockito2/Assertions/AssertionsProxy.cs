using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NMockito2.Assertions {
   public class AssertionsProxy {
      public void AssertEquals<T>(T expected, T actual) => Assert.Equal(expected, actual);

      public void AssertTrue(bool value) => Assert.True(value);
      public void AssertFalse(bool value) => Assert.False(value);
   }
}
