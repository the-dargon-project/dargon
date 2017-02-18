using System;
using Castle.DynamicProxy;
using NMockito.Utilities;

namespace NMockito.Mocks {
   public class MockInterceptor : IInterceptor {
      private readonly InvocationDescriptorFactory invocationDescriptorFactory;
      private readonly InvocationTransformer invocationTransformer;
      private readonly InvocationStage invocationStage;
      private readonly InvocationOperationManagerFinder invocationOperationManagerFinder;
      private readonly bool isTracked;

      public MockInterceptor(
         InvocationDescriptorFactory invocationDescriptorFactory, 
         InvocationTransformer invocationTransformer, 
         InvocationStage invocationStage, 
         InvocationOperationManagerFinder invocationOperationManagerFinder,
         bool isTracked) {
         this.invocationDescriptorFactory = invocationDescriptorFactory;
         this.invocationTransformer = invocationTransformer;
         this.invocationStage = invocationStage;
         this.invocationOperationManagerFinder = invocationOperationManagerFinder;
         this.isTracked = isTracked;
      }

      public bool IsTracked => isTracked;

      public void Intercept(IInvocation invocation) {
         // Transform and stage invocation
         var invocationDescriptor = invocationDescriptorFactory.Create(this, invocation);
         invocationTransformer.Forward(invocationDescriptor);
         invocationStage.SetLastInvocation(invocationDescriptor.Clone());

         // Determine return value of invocation
         InvocationOperationManager invocationOperationManager;
         if (invocationOperationManagerFinder.TryFind(invocationDescriptor, out invocationOperationManager)) {
            invocationOperationManager.Execute(invocationDescriptor);
         } else if (invocationDescriptor.Target != null) {
            invocation.ReturnValue = invocation.Method.Invoke(invocationDescriptor.Target, invocation.Arguments);
         } else {
            invocation.ReturnValue = invocation.Method.GetDefaultReturnValue();
         }

         // Reverse transform e.g. to set out params
         invocationTransformer.Backward(invocationDescriptor);

         // Throw if exception has been set
         invocationDescriptor.Exception?.Rethrow();
      }
   }
}