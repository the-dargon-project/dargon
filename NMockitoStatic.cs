using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;

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

      public static object CreateMock(Type t) 
      { 
         var factory = createMockGenericDefinition.MakeGenericMethod(new[] { t });
         return factory.Invoke(null, null);
      }

      public static T CreateMock<T>()
         where T : class
      {
         var state = new MockState(typeof(T));
         var interceptor = new MockInvocationInterceptor(state);
         var mock = proxyGenerator.CreateInterfaceProxyWithoutTarget<T>(interceptor);
         statesByMock.Add(mock, state);
         return mock;
      }

      public static T Verify<T>(T mock, INMockitoTimesMatcher times = null)
         where T : class
      {
         times = times ?? new NMockitoTimesAnyMatcher();

         var state = statesByMock[mock];
         var interceptor = new MockVerifyInterceptor(state, times);
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

         public MockVerifyInterceptor(MockState state, INMockitoTimesMatcher times)
         {
            this.state = state;
            this.times = times;
         }

         public void Intercept(IInvocation invocation) { state.HandleMockVerification(invocation, times); }
      }
   }
}