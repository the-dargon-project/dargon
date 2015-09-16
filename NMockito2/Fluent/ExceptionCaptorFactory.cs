using Castle.DynamicProxy;

namespace NMockito2.Fluent {
   public class ExceptionCaptorFactory {
      private readonly ProxyGenerator proxyGenerator;

      public ExceptionCaptorFactory(ProxyGenerator proxyGenerator) {
         this.proxyGenerator = proxyGenerator;
      }

      public TMock Create<TMock>(TMock mock, FluentExceptionAssertor fluentExceptionAssertor)
         where TMock : class {
         return proxyGenerator.CreateInterfaceProxyWithoutTarget<TMock>(
            new AssertExceptionCatchingInterceptor<TMock>(mock, fluentExceptionAssertor));
      }
   }
}