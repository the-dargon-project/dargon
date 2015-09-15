using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using Xunit;

namespace NMockito2.Tests {
   public class MockFactoryTests : NMockitoInstance {
      [Fact]
      public void Run() {
         var mock = CreateMock<X<string, int>>();
         mock.Func<bool>(false);
         mock.Func<string>("asdf");
      }

      public interface X<Y, Z> {
         W Func<W>(W w);
      }
   }
}
