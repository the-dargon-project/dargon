using Castle.DynamicProxy;

namespace NMockito.Fluent {
   public class ExceptionCaptorFactory {
      private readonly ProxyGenerator proxyGenerator;

      public ExceptionCaptorFactory(ProxyGenerator proxyGenerator) {
         this.proxyGenerator = proxyGenerator;
      }

      public TMock Create<TMock>(TMock mock, FluentExceptionAssertor fluentExceptionAssertor)
         where TMock : class {
         if (typeof(TMock).IsInterface) {
            return proxyGenerator.CreateInterfaceProxyWithoutTarget<TMock>(
               new AssertExceptionCatchingInterceptor<TMock>(mock, fluentExceptionAssertor));
         } else {
            return proxyGenerator.CreateClassProxy<TMock>(
               new AssertExceptionCatchingInterceptor<TMock>(mock, fluentExceptionAssertor));
         }
      }
   }
}