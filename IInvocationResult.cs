using System;

namespace ItzWarty.Test
{
   internal interface IInvocationResult
   {
      object GetValueOrThrow();
   }

   internal class InvocationThrowResult : IInvocationResult
   {
      private readonly Exception exception;
      public InvocationThrowResult(Exception exception) { this.exception = exception; }
      public object GetValueOrThrow() { throw exception; }
   }

   internal class InvocationReturnResult : IInvocationResult
   {
      private readonly object value;
      public InvocationReturnResult(object value) { this.value = value; }
      public object GetValueOrThrow() { return value; }
   }
}