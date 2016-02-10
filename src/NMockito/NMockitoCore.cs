using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using NMockito.Assertions;
using NMockito.Expectations;

namespace NMockito {
   public interface NMockitoCore {
      void InitializeMocks(object testClassInstance);

      object CreateMock(Type type);
      T CreateMock<T>() where T : class;
      T CreateMock<T>(Expression<Func<T, bool>> setupExpectations) where T : class;
      object CreateUntrackedMock(Type type);
      T CreateUntrackedMock<T>() where T : class;
      T CreateSpy<T>() where T : class;
      T CreateUntrackedSpy<T>() where T : class;
      object CreatePlaceholder(Type type);
      T CreatePlaceholder<T>();

      T Any<T>();

      Expectation When(Action action);
      Expectation<TResult> When<TResult>(TResult value);
      Expectation<TOut1, TResult> When<TOut1, TResult>(Func<TOut1, TResult> func);
      Expectation<TOut1, TOut2, TResult> When<TOut1, TResult, TOut2>(Func<TOut1, TOut2, TResult> func);
      Expectation<TOut1, TOut2, TOut3, TResult> When<TOut1, TOut2, TOut3, TResult>(Func<TOut1, TOut2, TOut3, TResult> func);

      Expectation Expect(Action action);
      Expectation<TResult> Expect<TResult>(TResult func);
      Expectation<TOut1, TResult> Expect<TOut1, TResult>(Func<TOut1, TResult> func);
      Expectation<TOut1, TOut2, TResult> Expect<TOut1, TOut2, TResult>(Func<TOut1, TOut2, TResult> func);
      Expectation<TOut1, TOut2, TOut3, TResult> Expect<TOut1, TOut2, TOut3, TResult>(Func<TOut1, TOut2, TOut3, TResult> func);

      void AssertEquals<T>(T expected, T actual);
      void AssertTrue(bool value);
      void AssertFalse(bool value);
      void AssertNull(object value);
      void AssertNotNull(object value);
      void AssertThrows<TException>(Action action) where TException : Exception;
      void AssertThrows<TOuterException, TInnerException>(Action action)
         where TOuterException : Exception
         where TInnerException : Exception;
      void AssertSequenceEquals<T>(IEnumerable<T> a, IEnumerable<T> b);
      AssertWithAction Assert(Action action);
      TMock Assert<TMock>(TMock mock) where TMock : class;

      void Verify(Action action);
      TMock Verify<TMock>(TMock mock) where TMock : class;
      void VerifyExpectations();
      void VerifyNoMoreInteractions();
      void VerifyExpectationsAndNoMoreInteractions();
   }
}