using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NMockito.Assertions;
using NMockito.Expectations;

namespace NMockito {
   public static class NMockitoStatics {
      private static NMockitoCore __core;

      private static NMockitoCore Core => __core = __core ?? NMockitoCoreImpl.Instance;

      public static void NoOp<T>(this T throwaway) { }

      public static void InitializeMocks(object testClass) => Core.InitializeMocks(testClass);

      public static object CreateMock(Type type) => Core.CreateMock(type);
      public static T CreateMock<T>() where T : class => Core.CreateMock<T>();
      public static T CreateMock<T>(Expression<Func<T, bool>> expectations) where T : class => Core.CreateMock(expectations);
      public static object CreateUntrackedMock(Type type) => Core.CreateUntrackedMock(type);
      public static T CreateUntrackedMock<T>() where T : class => Core.CreateUntrackedMock<T>();
      public static T CreateSpy<T>() where T : class => Core.CreateSpy<T>();
      public static T CreateUntrackedSpy<T>() where T : class => Core.CreateUntrackedSpy<T>();

      public static object CreatePlaceholder(Type type) => Core.CreatePlaceholder(type);
      public static T CreatePlaceholder<T>() => Core.CreatePlaceholder<T>();

      public static T Any<T>() => Core.Any<T>();

      public static Expectation When(Action action) => Core.When(action);
      public static Expectation<TResult> When<TResult>(TResult value) => Core.When(value);
      public static Expectation<TOut1, TResult> When<TOut1, TResult>(Func<TOut1, TResult> func) => Core.When(func);
      public static Expectation<TOut1, TOut2, TResult> When<TOut1, TResult, TOut2>(Func<TOut1, TOut2, TResult> func) => Core.When(func);
      public static Expectation<TOut1, TOut2, TOut3, TResult> When<TOut1, TOut2, TOut3, TResult>(Func<TOut1, TOut2, TOut3, TResult> func) => Core.When(func);

      public static Expectation Expect(Action action) => Core.Expect(action);
      public static Expectation<TResult> Expect<TResult>(TResult func) => Core.Expect(func);
      public static Expectation<TOut1, TResult> Expect<TOut1, TResult>(Func<TOut1, TResult> func) => Core.Expect(func);
      public static Expectation<TOut1, TOut2, TResult> Expect<TOut1, TOut2, TResult>(Func<TOut1, TOut2, TResult> func) => Core.Expect(func);
      public static Expectation<TOut1, TOut2, TOut3, TResult> Expect<TOut1, TOut2, TOut3, TResult>(Func<TOut1, TOut2, TOut3, TResult> func) => Core.Expect(func);

      public static void AssertEquals<T>(T expected, T actual) => Core.AssertEquals(expected, actual);
      public static void AssertTrue(bool value) => Core.AssertTrue(value);
      public static void AssertFalse(bool value) => Core.AssertFalse(value);
      public static void AssertNull(object value) => Core.AssertNull(value);
      public static void AssertNotNull(object value) => Core.AssertNotNull(value);
      public static void AssertThrows<TException>(Action action) where TException : Exception => Core.AssertThrows<TException>(action);
      public static void AssertSequenceEquals<T>(IEnumerable<T> a, IEnumerable<T> b) => Core.AssertSequenceEquals(a, b);
      public static AssertWithAction Assert(Action action) => Core.Assert(action);

      public static TMock Assert<TMock>(TMock mock) where TMock : class => Core.Assert(mock);

      public static void Verify(Action action) => Core.Verify(action);
      public static TMock Verify<TMock>(TMock mock) where TMock : class => Core.Verify(mock);

      public static void VerifyExpectations() => Core.VerifyExpectations();
      public static void VerifyNoMoreInteractions() => Core.VerifyNoMoreInteractions();
      public static void VerifyExpectationsAndNoMoreInteractions() => Core.VerifyExpectationsAndNoMoreInteractions();
   }
}
