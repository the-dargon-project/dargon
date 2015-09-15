using Castle.DynamicProxy;

namespace NMockito2.Mocks {
   public interface MockFactory {
      T CreateMock<T>() where T : class;
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
         var interceptor = new MockInterceptor(invocationDescriptorFactory, invocationTransformer, invocationStage, invocationOperationManagerFinder);
         var mock = proxyGenerator.CreateInterfaceProxyWithoutTarget<T>(interceptor);
         interceptor.SetMock(typeof(T), mock);
         return mock;
      }
   }
}