using System;
using Castle.DynamicProxy;

namespace NMockito2.Mocks {
   public interface MockFactory {
      object CreateMock(Type mockType);
      T CreateMock<T>() where T : class;

      T CreateSpy<T>() where T : class;
   }

   public class MockFactoryImpl : MockFactory {
      private readonly ProxyGenerator proxyGenerator;
      private readonly InvocationDescriptorFactory invocationDescriptorFactory;
      private readonly InvocationTransformer invocationTransformer;
      private readonly InvocationStage invocationStage;
      private readonly InvocationOperationManagerFinder invocationOperationManagerFinder;

      public MockFactoryImpl(ProxyGenerator proxyGenerator, InvocationDescriptorFactory invocationDescriptorFactory, InvocationTransformer invocationTransformer, InvocationStage invocationStage, InvocationOperationManagerFinder invocationOperationManagerFinder) {
         this.proxyGenerator = proxyGenerator;
         this.invocationDescriptorFactory = invocationDescriptorFactory;
         this.invocationTransformer = invocationTransformer;
         this.invocationStage = invocationStage;
         this.invocationOperationManagerFinder = invocationOperationManagerFinder;
      }

      public T CreateMock<T>() where T : class {
         return (T)CreateMock(typeof(T));
      }

      public object CreateMock(Type mockType) {
         var interceptor = new MockInterceptor(invocationDescriptorFactory, invocationTransformer, invocationStage, invocationOperationManagerFinder);
         var mock = proxyGenerator.CreateInterfaceProxyWithoutTarget(mockType, interceptor);
         return mock;
      }

      public T CreateSpy<T>() where T : class {
         var interceptor = new MockInterceptor(invocationDescriptorFactory, invocationTransformer, invocationStage, invocationOperationManagerFinder);
         T target = (T)Activator.CreateInstance(typeof(T));
         var mock = proxyGenerator.CreateClassProxyWithTarget(target, interceptor);
         return mock;
      }
   }
}