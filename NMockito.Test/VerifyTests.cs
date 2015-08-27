using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NMockito {
   public class VerifyTests : NMockitoInstance {
      [Fact]
      public void IncorrectParameter_Test() {
         var mock = CreateMock<DummyInterface>();
         mock.A();
         mock.B(10);
         try {
            Verify(mock).B(20);
            throw new Exception("It didn't throw!");
         } catch (Exception e) {
            var message = e.Message.Replace("\r", "").Replace("\n", "");
            AssertEquals("Expected > 0 invocations of B<Int32>( Int32 20 ) but found 0 invocations.Other invocations found:1 invocations of A(  )1 invocations of B<Int32>( Int32 10 )", message.Trim());
         }
      }

      [Fact]
      public void CallsRemaining_Test() {
         var mock = CreateMock<DummyInterface>();
         mock.A();
         try {
            VerifyNoMoreInteractions();
            throw new Exception("It didn't throw!");
         } catch (AggregateException ae) {
            AssertEquals(1, ae.InnerExceptions.Count);
            var e = ae.InnerExceptions[0];
            var message = e.Message.Replace("\r", "").Replace("\n", "");
            AssertEquals("Expected no more invocations of any method but found 1 invocations.Other invocations found:1 invocations of A(  )", message.Trim());
         }
      }

      public interface DummyInterface {
         void A();
         void B<T>(T x);
      }
   }
}
