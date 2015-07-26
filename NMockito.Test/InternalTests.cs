using System;
using Castle.DynamicProxy.Generators;
using Xunit;
using static NMockito.NMockitoStatic;

namespace NMockito {
   /// <summary>
   /// Note: Internal interface mocking is dependent on placing:
   ///    [assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
   /// In assemblyinfo.cs of the library containing the internal assembly.
   /// </summary>
   public class InternalTests {
      [Fact]
      public void InternalInterfaceMockable() {
         var internalInstance = CreateMock<InternalInterface>();
         When(internalInstance.One).ThenReturn(1);
         AssertEquals(1, internalInstance.One);
         Verify(internalInstance).One.ToString();
         VerifyNoMoreInteractions();
      }

      [Fact]
      public void PrivateInterfaceNotMockable() {
         AssertThrows<GeneratorException>(() => CreateMock<PrivateInterface>());
      }

      internal interface InternalInterface {
         int One { get; }
      }

      private interface PrivateInterface {
         int One { get; }
      }
   }
}
