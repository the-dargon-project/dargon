using System.Diagnostics;
using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Xunit;

namespace NMockito
{
   public static class NMockitoStatic
   {
      private static readonly ProxyGenerator proxyGenerator = new ProxyGenerator();
      private static readonly Dictionary<object, MockState> statesByMock = new Dictionary<object, MockState>();
      private static readonly MethodInfo createMockGenericDefinition;

      static NMockitoStatic()
      {
         var type = typeof(NMockitoStatic);
         var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
         createMockGenericDefinition = methods.First(info => info.IsGenericMethodDefinition && info.Name.StartsWith("CreateMock"));
      }

      public static void ReinitializeMocks(object self) {
         ClearInteractions();
         NMockitoAttributes.InitializeMocks(self);
      }

      public static object CreateMock(Type t, bool tracked = true) 
      { 
         var factory = createMockGenericDefinition.MakeGenericMethod(new[] { t });
         return factory.Invoke(null, new object[] { tracked });
      }

      public static T CreateMock<T>(bool tracked = true)
         where T : class
      {
         var state = new MockState(typeof(T));
         var interceptor = new MockInvocationInterceptor(state);
         var mock = proxyGenerator.CreateInterfaceProxyWithoutTarget<T>(interceptor);
         if (tracked) {
            statesByMock.Add(mock, state);
         }
         return mock;
      }
      
      public static T CreateUntrackedMock<T>() where T : class { return CreateMock<T>(false); }
      public static object CreateUntrackedMock(Type type) { return CreateMock(type, false); }

      public static T CreateRef<T>() 
         where T : class {
         return CreateMock<T>(false);
      }

      public static INMockitoTimesMatcher AnyTimes() { return new NMockitoTimesAnyMatcher(); }
      public static INMockitoTimesMatcher AnyOrNoneTimes() { return new NMockitoTimesAnyOrNoneMatcher(); }
      public static INMockitoTimesMatcher Times(int count) { return new NMockitoTimesEqualMatcher(count); }
      public static INMockitoTimesMatcher Never() { return Times(0); }
      public static INMockitoTimesMatcher Once() { return Times(1); }

      public static WhenContext<object> When(Expression<Action> expression) {
         expression.Compile().Invoke();
         return new WhenContext<object>();
      }

      public static WhenContext<T> When<T>(T value) { return new WhenContext<T>(); }

      public static T Verify<T>(T mock, INMockitoTimesMatcher times = null, NMockitoOrder order = NMockitoOrder.DontCare)
         where T : class
      {
         times = times ?? new NMockitoTimesAnyMatcher();

         var state = statesByMock[mock];
         var interceptor = new MockVerifyInterceptor(state, times, order);
         var proxy = proxyGenerator.CreateInterfaceProxyWithoutTarget<T>(interceptor);
         return proxy;
      }

      public static void VerifyNoMoreInteractions()
      {
         foreach (var state in statesByMock.Values) {
            state.VerifyNoMoreInteractions();
         }
      }

      public static void VerifyNoMoreInteractions<T>(T mock) { statesByMock[mock].VerifyNoMoreInteractions(); }

      public static void ClearInteractions()
      {
         foreach (var state in statesByMock.Values) {
            state.ClearInteractions();
         }
      }

      public static void ClearInteractions<T>(T mock)
      {
         MockState state;
         if (statesByMock.TryGetValue(mock, out state)) 
            state.ClearInteractions();
      }

      public static void ClearInteractions<T>(T mock, int expectedCount) { statesByMock[mock].ClearInteractions(expectedCount); }

      public static void AssertEquals<T>(T expected, T actual) { if (!Equals(expected, actual)) Assert.Equal(expected, actual); }
      public static void AssertNotEquals<T>(T expected, T actual) { if (Equals(expected, actual)) Assert.NotEqual(expected, actual); }
      public static void AssertNull<T>(T value) { Assert.Null(value); }
      public static void AssertNotNull<T>(T value) { Assert.NotNull(value); }
      public static void AssertTrue(bool value) { Assert.True(value); }
      public static void AssertFalse(bool value) { Assert.False(value); }
      public static void AssertThrows<TException>(Action x) where TException : Exception { Assert.Throws<TException>(new Assert.ThrowsDelegate(x)); }

      public static NMockitoOrder DontCare() { return NMockitoOrder.DontCare; }
      public static NMockitoOrder WithPrevious() { return NMockitoOrder.WithPrevious; }
      public static NMockitoOrder AfterPrevious() { return NMockitoOrder.AfterPrevious; }
      public static NMockitoOrder Whenever() { return NMockitoOrder.Whenever; }

      private class MockInvocationInterceptor : IInterceptor
      {
         private MockState state;
         public MockInvocationInterceptor(MockState state) { this.state = state; }
         public void Intercept(IInvocation invocation) { state.HandleMockInvocation(invocation); }
      }


      private class MockVerifyInterceptor : IInterceptor
      {
         private readonly MockState state;
         private readonly INMockitoTimesMatcher times;
         private readonly NMockitoOrder order;

         public MockVerifyInterceptor(MockState state, INMockitoTimesMatcher times, NMockitoOrder order)
         {
            this.state = state;
            this.times = times;
            this.order = order;
         }

         public void Intercept(IInvocation invocation) { state.HandleMockVerification(invocation, times, order); }
      }
   }
}