using System;
using Xunit;
using NMockito2.Fluent;

namespace NMockito2 {
   public class MockFactoryTests : NMockitoInstance {
      [Fact]
      public void Run() {
         var mock = CreateMock<X<string, int>>();
         Console.WriteLine(mock.Func(Any<bool>()).Returns(true).Returns(false));
         Console.WriteLine(mock.Func("Hello").Returns("asdf").Returns("jkl"));

         Console.WriteLine(mock.Func(false));
         Console.WriteLine(mock.Func(false));

         Console.WriteLine(mock.Func("Hello"));
         Console.WriteLine(mock.Func("Hello"));
         mock.Func<string>("asdf");
      }

      public interface X<Y, Z> {
         W Func<W>(W w);
      }
   }
}
