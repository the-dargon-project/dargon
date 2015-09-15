using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace NMockito2.Utilities {
   public class ReflectionUtilitiesTests {
      [Fact]
      public void GetDefaultValueTests() {
         Assert.Equal(0, typeof(int).GetDefaultValue());
         Assert.Equal(false, typeof(bool).GetDefaultValue());
         Assert.Equal(TimeSpan.Zero, typeof(TimeSpan).GetDefaultValue());
         Assert.Equal(null, typeof(string).GetDefaultValue());
         Assert.Equal(null, typeof(IDictionary<,>).GetDefaultValue());
         Assert.Equal(null, typeof(Dictionary<,>).GetDefaultValue());
         Assert.Equal(null, typeof(Func<,>).GetDefaultValue());
         Assert.Equal(null, typeof(int?).GetDefaultValue());
         Assert.Equal(null, typeof(object).MakeByRefType().GetDefaultValue());
      }

      [Fact]
      public void TryGetParamsType_HappyPathTest() {
         var paramsExampleMethod = typeof(TestClass).GetMethod(nameof(TestClass.ParamsExample));
         Type paramsArrayType;
         Assert.True(paramsExampleMethod.TryGetParamsType(out paramsArrayType));
         Assert.Equal(typeof(string[]), paramsArrayType);
      }

      [Fact]
      public void TryGetParamsType_SadPathTest() {
         var copyExampleMethod = typeof(string).GetMethod(nameof(string.Copy));
         Type paramsArrayType;
         Assert.False(copyExampleMethod.TryGetParamsType(out paramsArrayType));
         Assert.Null(paramsArrayType);
      }

      private static class TestClass {
         public static void ParamsExample(int i, params string[] x) { }
      }
   }
}