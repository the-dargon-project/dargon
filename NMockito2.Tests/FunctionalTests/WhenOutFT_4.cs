using System;
using NMockito2.Fluent;
using Xunit;

namespace NMockito2.FunctionalTests {
   public class WhenOutFT_4 : NMockitoInstance {
      [Fact]
      public void Run() {
         var obj = new object();

         var testObj = CreateMock<TestInterface>();
         Expect<int, char, object, bool>((x, y, z) => testObj.TryInvoke("asdf", out x, out y, out z))
            .SetOut(1337, 't', obj).ThenReturn(true)
            .SetOut(21337, 'u', null).ThenReturn(false)
            .ThenThrow(new InvalidOperationException());

         int a;
         char b;
         object c;
         AssertTrue(testObj.TryInvoke("asdf", out a, out b, out c));
         AssertEquals(1337, a);
         AssertEquals('t', b);
         AssertEquals(obj, c);

         AssertFalse(testObj.TryInvoke("asdf", out a, out b, out c));
         AssertEquals(21337, a);
         AssertEquals('u', b);
         AssertEquals(null, c);

         Assert(testObj).TryInvoke("asdf", out a, out b, out c).Throws<InvalidOperationException>();

         VerifyExpectationsAndNoMoreInteractions();
      }

      internal interface TestInterface {
         bool TryInvoke(string s, out int a, out char b, out object c);
      }
   }
}
