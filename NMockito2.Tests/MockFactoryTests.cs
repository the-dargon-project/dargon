using System;
using Xunit;
using NMockito2.Fluent;

namespace NMockito2 {
   public class MockFactoryTests : NMockitoInstance {
      [Fact]
      public void Run() {
         var mock = CreateMock<X<string, int>>();
         Console.WriteLine(mock.Func(Any<bool>()).Returns(true).ThenReturns(false));
         Console.WriteLine(mock.Func("Hello").Returns("asdf").ThenReturns("jkl"));

         mock.Func(false).IsTrue();
         mock.Func(false).IsFalse();

         mock.Func("Hello").IsEqualTo("asdf");
         mock.Func("Hello").IsEqualTo("jkl");
         mock.Func<string>("asdf").IsEqualTo(null);
      }

      public interface X<Y, Z> {
         W Func<W>(W w);
      }
   }
}
