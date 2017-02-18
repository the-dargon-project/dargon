using System;
using NMockito.Fluent;
using Xunit;
using Xunit.Sdk;

namespace NMockito.FunctionalTests {
   public class FluentAssertionFT : NMockitoInstance {
      [Fact]
      public void Run() {
         var mock = CreateMock<TestInterface>();
         mock.Run(10)
             .Returns("asdf", "jkl", "querty")
             .ThenThrows(new InvalidOperationException());

         mock.Run(10).IsEqualTo("asdf");
         Assert(mock).Run(10).IsEqualTo("jkl");
         AssertThrows<ThrowsException>(() => { Assert(mock).Run(10).Throws<ArgumentException>(); });
         Assert(mock).Run(10).Throws<InvalidOperationException>();
         AssertThrows<ThrowsException>(() => Assert(mock).Run(10).Throws<ArgumentException>());
         AssertThrows<InvalidOperationException>(() => { mock.Run(10); });

         VerifyExpectationsAndNoMoreInteractions();
      }

      internal interface TestInterface {
         string Run(int x);
      }
   }
}
