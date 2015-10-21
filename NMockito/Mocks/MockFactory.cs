using System;
using Castle.DynamicProxy;

namespace NMockito.Mocks {
   public interface MockFactory {
      object CreateMock(Type mockType);
      T CreateMock<T>() where T : class;

      object CreateUntrackedMock(Type mockType);
      T CreateUntrackedMock<T>() where T : class;

      object CreateSpy(Type spyType);
      T CreateSpy<T>() where T : class;

      object CreateUntrackedSpy(Type spyType);
      T CreateUntrackedSpy<T>() where T : class;

      object Create(Type mockType, bool isTracked);
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

      public object CreateMock(Type mockType) {
         return Create(mockType, true);
      }

      public T CreateMock<T>() where T : class {
         return (T)CreateMock(typeof(T));
      }

      public object CreateUntrackedMock(Type mockType) {
         return Create(mockType, false);
      }

      public T CreateUntrackedMock<T>() where T : class {
         return (T)CreateUntrackedMock(typeof(T));
      }

      public object CreateSpy(Type spyType) {
         return Create(spyType, true);
      }

      public T CreateSpy<T>() where T : class {
         return (T)CreateSpy(typeof(T));
      }

      public object CreateUntrackedSpy(Type spyType) {
         return Create(spyType, false);
      }

      public T CreateUntrackedSpy<T>() where T : class {
         return (T)CreateUntrackedSpy(typeof(T));
      }

      public object Create(Type mockType, bool isTracked) {
         var interceptor = new MockInterceptor(invocationDescriptorFactory, invocationTransformer, invocationStage, invocationOperationManagerFinder, isTracked);
         if (mockType.IsInterface) {
            return proxyGenerator.CreateInterfaceProxyWithoutTarget(mockType, interceptor);
         } else {
            var target = Activator.CreateInstance(mockType);
            return proxyGenerator.CreateClassProxyWithTarget(mockType, target, interceptor);
         }
      }
   }
}