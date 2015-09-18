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
         return Create(mockType);
      }

      public T CreateSpy<T>() where T : class {
         return (T)CreateSpy(typeof(T));
      }

      public object CreateSpy(Type spyType) {
         return Create(spyType);
      }

      public object Create(Type mockType) {
         var interceptor = new MockInterceptor(invocationDescriptorFactory, invocationTransformer, invocationStage, invocationOperationManagerFinder);
         if (mockType.IsInterface) {
            return proxyGenerator.CreateInterfaceProxyWithoutTarget(mockType, interceptor);
         } else {
            var target = Activator.CreateInstance(mockType);
            return proxyGenerator.CreateClassProxyWithTarget(mockType, target, interceptor);
         }
      }
   }
}