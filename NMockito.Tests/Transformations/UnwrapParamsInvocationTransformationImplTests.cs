using System.Linq;
using System.Reflection;
using NMockito.Mocks;
using Xunit;

namespace NMockito.Transformations {
   public class UnwrapParamsInvocationTransformationImplTests {
      private readonly MethodInfo paramslessMethod = typeof(TestClass).GetMethod(nameof(TestClass.Paramsless), BindingFlags.Instance | BindingFlags.Public);
      private readonly MethodInfo paramsfulMethod = typeof(TestClass).GetMethod(nameof(TestClass.Paramsful), BindingFlags.Instance | BindingFlags.Public);
      private readonly UnwrapParamsInvocationTransformationImpl testObj;

      public UnwrapParamsInvocationTransformationImplTests() {
         this.testObj = new UnwrapParamsInvocationTransformationImpl();
      }

      [Fact]
      public void IsApplicable_GivenParamslessMethod_ReturnsFalse() {
         var invocationDescriptor = new InvocationDescriptor {
            Arguments = new object[] { 2, new[] { "asdf" } },
            Method = paramslessMethod
         };
         Assert.False(testObj.IsApplicable(invocationDescriptor));
      }

      [Fact]
      public void IsApplicable_GivenParamsfulMethodWithNullParams_ReturnsFalse() {
         var invocationDescriptor = new InvocationDescriptor {
            Arguments = new object[] { 2, null },
            Method = paramsfulMethod
         };
         Assert.False(testObj.IsApplicable(invocationDescriptor));
      }

      [Fact]
      public void IsApplicable_GivenParamsfulMethodWithNonNullParams_ReturnsTrue() {
         var invocationDescriptor = new InvocationDescriptor {
            Arguments = new object[] { 2, new[] { "asdf" } },
            Method = paramsfulMethod
         };
         Assert.True(testObj.IsApplicable(invocationDescriptor));
      }

      [Fact]
      public void Forward_FlattensParamsArgument_HappyPathTest() {
         var invocationDescriptor = new InvocationDescriptor {
            Arguments = new object[] { 2, new[] { "asdf", "jkl" } },
            Method = paramsfulMethod
         };
         testObj.Forward(invocationDescriptor);
         Assert.True(new object[] { 2, "asdf", "jkl" }.SequenceEqual(invocationDescriptor.Arguments));
      }

      [Fact]
      public void Backward_UnflattensParamsArgument_HappyPathTest() {
         var invocationDescriptor = new InvocationDescriptor {
            Arguments = new object[] { 5,  "asdf", "jkl" },
            Method = paramsfulMethod
         };
         testObj.Backward(invocationDescriptor);
         Assert.Equal(2, invocationDescriptor.Arguments.Length);
         Assert.Equal(5, invocationDescriptor.Arguments[0]);
         Assert.True(new object[] { "asdf", "jkl" }.SequenceEqual((string[])invocationDescriptor.Arguments[1]));
      }

      private class TestClass {
         public void Paramsless(int a, string[] b) { }
         public void Paramsful(int a , params string[] b) { }
      }
   }
}