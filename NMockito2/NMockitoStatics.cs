using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMockito2.Assertions;
using NMockito2.Expectations;

namespace NMockito2 {
   public static class NMockitoStatics {
      private static object synchronization = new object();
      private static NMockitoCore core;

      public static void InitializeMocks(object testClass) {
         core = NMockitoInstance.Instance;
      }

      public static void NoOp<T>(this T throwaway) { }

      public static object CreateMock(Type type) => core.CreateMock(type);
      public static T CreateMock<T>() where T : class => core.CreateMock<T>();
      public static T CreateSpy<T>() where T : class => core.CreateSpy<T>();

      public static object CreatePlaceholder(Type type) => core.CreatePlaceholder(type);
      public static T CreatePlaceholder<T>() => core.CreatePlaceholder<T>();

      public static T Any<T>() => core.Any<T>();

      public static Expectation When(Action action) => core.When(action);
      public static Expectation<TResult> When<TResult>(TResult value) => core.When(value);
      public static Expectation<TOut1, TResult> When<TOut1, TResult>(Func<TOut1, TResult> func) => core.When(func);
      public static Expectation<TOut1, TOut2, TResult> When<TOut1, TResult, TOut2>(Func<TOut1, TOut2, TResult> func) => core.When(func);
      public static Expectation<TOut1, TOut2, TOut3, TResult> When<TOut1, TOut2, TOut3, TResult>(Func<TOut1, TOut2, TOut3, TResult> func) => core.When(func);

      public static Expectation Expect(Action action) => core.Expect(action);
      public static Expectation<TResult> Expect<TResult>(TResult func) => core.Expect(func);
      public static Expectation<TOut1, TResult> Expect<TOut1, TResult>(Func<TOut1, TResult> func) => core.Expect(func);
      public static Expectation<TOut1, TOut2, TResult> Expect<TOut1, TOut2, TResult>(Func<TOut1, TOut2, TResult> func) => core.Expect(func);
      public static Expectation<TOut1, TOut2, TOut3, TResult> Expect<TOut1, TOut2, TOut3, TResult>(Func<TOut1, TOut2, TOut3, TResult> func) => core.Expect(func);

      public static void AssertEquals<T>(T expected, T actual) => core.AssertEquals(expected, actual);
      public static void AssertTrue(bool value) => core.AssertTrue(value);
      public static void AssertFalse(bool value) => core.AssertFalse(value);
      public static void AssertNull(object value) => core.AssertNull(value);
      public static void AssertNotNull(object value) => core.AssertNotNull(value);
      public static void AssertThrows<TException>(Action action) where TException : Exception => core.AssertThrows<TException>(action);
      public static AssertWithAction Assert(Action action) => core.Assert(action);

      public static TMock Assert<TMock>(TMock mock) where TMock : class => core.Assert(mock);

      public static void Verify(Action action) => core.Verify(action);
      public static TMock Verify<TMock>(TMock mock) where TMock : class => core.Verify(mock);

      public static void VerifyExpectations() => core.VerifyExpectations();
      public static void VerifyNoMoreInteractions() => core.VerifyNoMoreInteractions();
      public static void VerifyExpectationsAndNoMoreInteractions() => core.VerifyExpectationsAndNoMoreInteractions();
   }
}
