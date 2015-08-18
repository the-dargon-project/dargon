using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMockito.Annotations;
using Xunit;

namespace NMockito {
   public class RefTests : NMockitoInstance {
      [Fact]
      public void Run_IntRefPath_Test() {
         var testObj = CreateMock<DummyInterface>();
         var originalIntPlaceholder = CreatePlaceholder<int>();
         var passedIntPlaceholder = originalIntPlaceholder;
         When(() => testObj.Derp(ref passedIntPlaceholder)).Set(passedIntPlaceholder, 1337).ThenReturn();
         testObj.Derp(ref passedIntPlaceholder);
         Verify(testObj).Derp(ref originalIntPlaceholder);
         VerifyNoMoreInteractions();
         AssertEquals(1337, passedIntPlaceholder);
      }

      [Fact]
      public void Run_ObjectRefPath_Test() {
         var testObj = CreateMock<DummyInterface>();
         var originalTargetObj = CreateMock<TargetInterface>();
         var passedTargetObj = originalTargetObj;
         When(() => testObj.Derp(ref passedTargetObj)).Exec(() => passedTargetObj.Invoke()).Set(passedTargetObj, null).ThenReturn();
         testObj.Derp(ref passedTargetObj);
         Verify(testObj).Derp(ref originalTargetObj);
         Verify(originalTargetObj).Invoke();
         VerifyNoMoreInteractions();
         AssertNull(passedTargetObj);
         AssertNotNull(originalTargetObj);
      }

      public interface DummyInterface {
         void Derp(ref int herp);
         void Derp(ref TargetInterface lerp);
      }

      public interface TargetInterface {
         void Invoke();
      }
   }
}
