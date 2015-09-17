using System;
using System.Collections.Generic;
using Castle.DynamicProxy;
using NMockito2.Transformations;

namespace NMockito2.Mocks {
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