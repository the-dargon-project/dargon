using System;
using NMockito.Fluent;
using Xunit;

namespace NMockito.FunctionalTests {
   public class SpyFT : NMockitoInstance {
      [Fact]
      public void Run() {
         var spy = CreateSpy<TestClass>();
         Expect<string, bool>(x => spy.TryGet(10, out x))
            .SetOut("qwerty").ThenReturn(true)
            .ThenThrow(new InvalidOperationException());

         string value;
         AssertTrue(spy.TryGet(10, out value));
         AssertEquals("qwerty", value);
         AssertEquals(3, spy.Three);
         AssertEquals(5, spy.Five);
         Assert(spy).TryGet(10, out value).Throws<InvalidOperationException>();

         Verify(spy).Five.NoOp();
         VerifyExpectationsAndNoMoreInteractions();
      }

      public class TestClass {
         public virtual bool TryGet(int key, out string value) {
            value = "asdf";
            return false;
         }

         public int Three => 3;
         public virtual int Five => 5;
      }
   }
}
