using Castle.DynamicProxy;
using Dargon.Commons;
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Dargon.Courier.ServiceTier.Vox;

namespace Dargon.Courier.ServiceTier.Client {
   public class RemoteServiceProxyInvocationInterceptor {
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