﻿using System;
using NMockito2.Fluent;
using Xunit;

namespace NMockito2.FunctionalTests {
   public class WhenOutFT_1 : NMockitoInstance {
      [Fact]
      public void Run() {
         var testObj = CreateMock<TestInterface>();
         testObj.TryInvoke("asdf").Returns(true, false);

         AssertTrue(testObj.TryInvoke("asdf"));
         AssertFalse(testObj.TryInvoke("asdf"));

         VerifyExpectationsAndNoMoreInteractions();
      }

      internal interface TestInterface {
         bool TryInvoke(string x);
      }
   }
}
