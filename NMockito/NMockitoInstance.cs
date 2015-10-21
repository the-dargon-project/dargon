using System;
using System.Collections.Generic;
using NMockito.Assertions;
using NMockito.Expectations;

namespace NMockito {
   public class NMockitoInstance : NMockitoCore {
      private readonly NMockitoCore core;

      public NMockitoInstance() {
         core = new NMockitoCoreImpl();

         InitializeMocks(this);
      }

      public void InitializeMocks(object testClassInstance) => core.InitializeMocks(testClassInstance);

      public object CreateMock(Type type) => core.CreateMock(type);
      public T CreateMock<T>() where T : class => core.CreateMock<T>();
      public T CreateMock<T>(Func<T, bool> setupExpectations) where T : class => core.CreateMock<T>();
      public T CreateSpy<T>() where T : class => core.CreateSpy<T>();
      public object CreatePlaceholder(Type type) => core.CreatePlaceholder(type);
      public T CreatePlaceholder<T>() => core.CreatePlaceholder<T>();

      public T Any<T>() => core.Any<T>();

      public Expectation When(Action action) => core.When(action);
      public Expectation<TResult> When<TResult>(TResult value) => core.When(value);
      public Expectation<TOut1, TResult> When<TOut1, TResult>(Func<TOut1, TResult> func) => core.When(func);
      public Expectation<TOut1, TOut2, TResult> When<TOut1, TResult, TOut2>(Func<TOut1, TOut2, TResult> func) => core.When(func);
      public Expectation<TOut1, TOut2, TOut3, TResult> When<TOut1, TOut2, TOut3, TResult>(Func<TOut1, TOut2, TOut3, TResult> func) => core.When(func);

      public Expectation Expect(Action action) => core.Expect(action);
      public Expectation<TResult> Expect<TResult>(TResult func) => core.Expect(func);
      public Expectation<TOut1, TResult> Expect<TOut1, TResult>(Func<TOut1, TResult> func) => core.Expect(func);
      public Expectation<TOut1, TOut2, TResult> Expect<TOut1, TOut2, TResult>(Func<TOut1, TOut2, TResult> func) => core.Expect(func);
      public Expectation<TOut1, TOut2, TOut3, TResult> Expect<TOut1, TOut2, TOut3, TResult>(Func<TOut1, TOut2, TOut3, TResult> func) => core.Expect(func);

      public void AssertEquals<T>(T expected, T actual) => core.AssertEquals(expected, actual);
      public void AssertTrue(bool value) => core.AssertTrue(value);
      public void AssertFalse(bool value) => core.AssertFalse(value);
      public void AssertNull(object value) => core.AssertNull(value);
      public void AssertNotNull(object value) => core.AssertNotNull(value);
      public void AssertThrows<TException>(Action action) where TException : Exception => core.AssertThrows<TException>(action);
      public void AssertSequenceEquals<T>(IEnumerable<T> a, IEnumerable<T> b) => core.AssertSequenceEquals(a, b);
      public AssertWithAction Assert(Action action) => core.Assert(action);
      public TMock Assert<TMock>(TMock mock) where TMock : class => core.Assert(mock);

      public void Verify(Action action) => core.Verify(action);
      public TMock Verify<TMock>(TMock mock) where TMock : class => core.Verify(mock);
      public void VerifyExpectations() => core.VerifyExpectations();
      public void VerifyNoMoreInteractions() => core.VerifyNoMoreInteractions();
      public void VerifyExpectationsAndNoMoreInteractions() => core.VerifyExpectationsAndNoMoreInteractions();
   }
}