using System;
using NMockito.Fluent;
using Xunit;

namespace NMockito {
   public class MockFactoryTests : NMockitoInstance {
      [Fact]
      public void Run() {
         var mock = CreateMock<IInvokable<string, int>>();
         mock.Func(Any<bool>()).Returns(true).ThenReturns(false);
         mock.Func("Hello").Returns("asdf").ThenReturns("jkl");

         mock.Func(false).IsTrue();
         mock.Func(false).IsFalse();

         mock.Func("Hello").IsEqualTo("asdf");
         mock.Func("Hello").IsEqualTo("jkl");
         mock.Func<string>("asdf").IsEqualTo(null);
      }

      public interface IInvokable<Y, Z> {
         W Func<W>(W w);
      }
   }
}
