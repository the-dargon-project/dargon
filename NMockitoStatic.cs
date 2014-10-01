using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using Moq;

namespace ItzWarty.Test
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

      public static T Verify<T>(T mock) 
      { 
         return mock; 
      }


      private class MockInvocationInterceptor : IInterceptor
      {
         private MockState state;

         public MockInvocationInterceptor(MockState state) { this.state = state; }

         public void Intercept(IInvocation invocation) { state.HandleMockInvocation(invocation); }
      }
   }
}