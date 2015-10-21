using System.Collections.Generic;
using Castle.DynamicProxy;
using NMockito.Transformations;

namespace NMockito.Mocks {
   public class InvocationDescriptorFactory {
      public InvocationDescriptor Create(IInvocation invocation) {
         return new InvocationDescriptor {
            Method = invocation.Method,
            Arguments = (object[])invocation.Arguments.Clone(),
            Invocation = invocation,
            Transformations = new List<InvocationTransformation>(),
            SmartParameters = null
         };
      }
   }
}