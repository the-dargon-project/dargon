using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NMockito {
   public class PlaceholderTests : NMockitoInstance {
      [Fact]
      public void CreatePlaceholder_OfClass_UsesDefaultConstructorTest() {
         var placeholder = CreatePlaceholder<DefaultConstructedClass>();
         AssertNotNull(placeholder);
         VerifyNoMoreInteractions();
      }

      [Fact]
      public void CreatePlaceholder_OfInterface_ReturnsUntrackedMockTest() {
         var placeholder = CreatePlaceholder<OutTests.Configuration>();
         When(placeholder.Validate()).ThenReturn(true);
         AssertTrue(placeholder.Validate());
         VerifyNoMoreInteractions();
      }

      [Fact]
      public void CreatePlaceholder_OfNumeric_ReturnsNonzeroTest() {
         AssertFalse(Enumerable.Range(0, 2000).Select(x => CreatePlaceholder<byte>()).Any(x => x == 0));
         AssertFalse(Enumerable.Range(0, 2000).Select(x => CreatePlaceholder<sbyte>()).Any(x => x == 0));
         AssertFalse(Enumerable.Range(0, 200000).Select(x => CreatePlaceholder<ushort>()).Any(x => x == 0));
         AssertFalse(Enumerable.Range(0, 200000).Select(x => CreatePlaceholder<short>()).Any(x => x == 0));
         AssertFalse(Enumerable.Range(0, 200000).Select(x => CreatePlaceholder<uint>()).Any(x => x == 0));
         AssertFalse(Enumerable.Range(0, 200000).Select(x => CreatePlaceholder<int>()).Any(x => x == 0));
         AssertFalse(Enumerable.Range(0, 200000).Select(x => CreatePlaceholder<ulong>()).Any(x => x == 0));
         AssertFalse(Enumerable.Range(0, 200000).Select(x => CreatePlaceholder<long>()).Any(x => x == 0));
      }

      public class DefaultConstructedClass {
         public DefaultConstructedClass() {
            Console.WriteLine("Default constructor invoked!");
         }
      }
   }
}
