using Castle.DynamicProxy;
using Dargon.Commons;
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Dargon.Courier.ServiceTier.Vox;

namespace Dargon.Courier.ServiceTier.Client {
   public class RemoteServiceProxyInvocationInterceptor : IInterceptor {
      private readonly RemoveServiceInfo remoteServiceInfo;
      private readonly RemoteServiceInvoker remoteServiceInvoker;

      public RemoteServiceProxyInvocationInterceptor(RemoveServiceInfo remoteServiceInfo, RemoteServiceInvoker remoteServiceInvoker) {
         this.remoteServiceInfo = remoteServiceInfo;
         this.remoteServiceInvoker = remoteServiceInvoker;
      }

      public void Intercept(IInvocation invocation) {
         RmiResponseDto responseDto;
         try {
            responseDto = InterceptAsync(invocation.Method, invocation.Arguments).Result;
         } catch (AggregateException ae) {
            if (ae.InnerExceptions.Count == 1) {
               ExceptionDispatchInfo.Capture(ae.InnerExceptions[0]).Throw();
               return; // unreachable
            } else {
               throw;
            }
         }

         invocation.ReturnValue = responseDto.ReturnValue;
         var parameters = invocation.Method.GetParameters();
         var outValues = responseDto.Outs;
         for (int i = 0, outIndex = 0; i < parameters.Length && outIndex < outValues.Length; i++) {
            if (parameters[i].IsOut) {
               invocation.Arguments[i] = outValues[outIndex++];
            }
         }
         var exception = responseDto.Exception as Exception;
         if (exception != null) {
            throw exception;
         }
      }

      public Task<RmiResponseDto> InterceptAsync(MethodInfo methodInfo, object[] methodArguments) {
         return remoteServiceInvoker.Invoke(
            remoteServiceInfo,
            methodInfo,
            methodArguments);
      }
   }
}