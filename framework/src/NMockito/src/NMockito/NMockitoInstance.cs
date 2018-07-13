using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using NMockito.Assertions;
using NMockito.Expectations;

namespace NMockito {
   public class NMockitoInstance : NMockitoCore {
      private static readonly object g_patchAppDomainManagerLock = new object(); // prevents NMIs of different testing threads from racing to patch appdomain manager.
      private readonly NMockitoCore core;

      public NMockitoInstance() {
         core = new NMockitoCoreImpl();

         InitializeMocks(this);
         PatchAssemblyGetEntryAssembly(Assembly.GetCallingAssembly());
      }

      private void PatchAssemblyGetEntryAssembly(Assembly entryAssembly) {
         lock (g_patchAppDomainManagerLock) {
            if (Assembly.GetEntryAssembly() != null) return;

            // via https://github.com/Microsoft/vstest/issues/649
            var manager = new AppDomainManager();
            var entryAssemblyField = manager.GetType().GetField("m_entryAssembly", BindingFlags.Instance | BindingFlags.NonPublic);
            if (entryAssemblyField == null) throw new Exception("Failed to use reflection on framework to set Assembly.GetEntryAssembly()!");
            entryAssemblyField.SetValue(manager, entryAssembly);

            var domain = AppDomain.CurrentDomain;
            var domainManagerField = domain.GetType().GetField("_domainManager", BindingFlags.Instance | BindingFlags.NonPublic);
            if (domainManagerField == null) throw new Exception("Failed to use reflection on framework to set Assembly.GetEntryAssembly()!");
            domainManagerField.SetValue(domain, manager);

            if (Assembly.GetEntryAssembly() != entryAssembly) {
               throw new Exception("Failed to set Assembly.GetEntryAssembly()!");
            }
         }
      }

      public void InitializeMocks(object testClassInstance) => core.InitializeMocks(testClassInstance);

      public object CreateMock(Type type) => core.CreateMock(type);
      public T CreateMock<T>() where T : class => core.CreateMock<T>();
      public T CreateMock<T>(Expression<Func<T, bool>> setupExpectations) where T : class => core.CreateMock(setupExpectations);
      public object CreateUntrackedMock(Type type) => core.CreateUntrackedMock(type);
      public T CreateUntrackedMock<T>() where T : class => core.CreateUntrackedMock<T>();
      public T CreateSpy<T>() where T : class => core.CreateSpy<T>();
      public T CreateUntrackedSpy<T>() where T : class => core.CreateUntrackedSpy<T>();

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
      public void AssertNotEquals<T>(T expected, T actual) => core.AssertNotEquals(expected, actual);
      public void AssertTrue(bool value) => core.AssertTrue(value);
      public void AssertFalse(bool value) => core.AssertFalse(value);
      public void AssertNull(object value) => core.AssertNull(value);
      public void AssertNotNull(object value) => core.AssertNotNull(value);
      public void AssertThrows<TException>(Action action) where TException : Exception => core.AssertThrows<TException>(action);
      public void AssertThrows<TOuterException, TInnerException>(Action action) where TOuterException : Exception where TInnerException : Exception => core.AssertThrows<TOuterException, TInnerException>(action);
      public void AssertSequenceEquals<T>(IEnumerable<T> a, IEnumerable<T> b) => core.AssertSequenceEquals(a, b);
      public void AssertCollectionDeepEquals<T>(IEnumerable<T> expected, IEnumerable<T> actual) => core.AssertCollectionDeepEquals(expected, actual);
      public void AssertDeepEquals<T>(T expected, T actual) => core.AssertDeepEquals(expected, actual);
      public AssertWithAction Assert(Action action) => core.Assert(action);
      public TMock Assert<TMock>(TMock mock) where TMock : class => core.Assert(mock);

      public void Verify(Action action) => core.Verify(action);
      public TMock Verify<TMock>(TMock mock) where TMock : class => core.Verify(mock);
      public void VerifyExpectations() => core.VerifyExpectations();
      public void VerifyNoMoreInteractions() => core.VerifyNoMoreInteractions();
      public void VerifyExpectationsAndNoMoreInteractions() => core.VerifyExpectationsAndNoMoreInteractions();
   }
}