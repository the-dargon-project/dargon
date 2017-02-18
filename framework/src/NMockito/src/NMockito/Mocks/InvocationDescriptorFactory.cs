using System.Collections.Generic;
using Castle.DynamicProxy;
using NMockito.Transformations;

namespace NMockito.Mocks {
   public class InvocationDescriptorFactory {
      public InvocationDescriptor Create(MockInterceptor interceptor, IInvocation invocation) {
         return new InvocationDescriptor {
            Interceptor = interceptor,
            Method = invocation.Method,
            Arguments = (object[])invocation.Arguments.Clone(),
            Invocation = invocation,
            Transformations = new List<InvocationTransformation>(),
            SmartParameters = null
         };
      }
   }
}