using System;
using System.Linq;
using Xunit;

namespace NMockito.Placeholders {
   public class PlaceholderFactoryTests : NMockitoInstance {
      [Fact]
      public void CreatePlaceholder_OfClass_UsesDefaultConstructorTest() {
         var placeholder = CreatePlaceholder<DefaultConstructedClass>();
         AssertNotNull(placeholder);
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
         AssertFalse(Enumerable.Range(0, 200000).Select(x => CreatePlaceholder<Guid>()).Any(x => x.Equals(Guid.Empty)));
      }

      [Fact]
      public void CreatePlaceholder_OfUnhandledType_ThrowsNotSupportedTest() {
         AssertThrows<NotSupportedException>(() => CreatePlaceholder(typeof(void)));
      }

      public class DefaultConstructedClass { }
   }
}