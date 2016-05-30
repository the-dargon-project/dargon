using Castle.DynamicProxy;
using Dargon.Commons;
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Dargon.Courier.ServiceTier.Vox;
using NLog;
using static Dargon.Commons.Channels.ChannelsExtensions;

namespace Dargon.Courier.ServiceTier.Client {
   public class RemoteServiceProxyInvocationInterceptor : IInterceptor {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      private readonly RemoveServiceInfo remoteServiceInfo;
      private readonly RemoteServiceInvoker remoteServiceInvoker;

      public RemoteServiceProxyInvocationInterceptor(RemoveServiceInfo remoteServiceInfo, RemoteServiceInvoker remoteServiceInvoker) {
         this.remoteServiceInfo = remoteServiceInfo;
         this.remoteServiceInvoker = remoteServiceInvoker;
      }

      public void Intercept(IInvocation invocation) {
         RmiResponseDto responseDto = null;
         try {
            Go(async () => {
               logger.Debug($"At intercept async for invocation on method {invocation.Method.Name} for service {remoteServiceInfo.ServiceType.Name}");
               responseDto = await InterceptAsync(invocation.Method, invocation.Arguments).ConfigureAwait(false);
               logger.Debug($"Completing Intercept async for invocation on method {invocation.Method.Name} for service {remoteServiceInfo.ServiceType.Name}");
            }).ConfigureAwait(false).GetAwaiter().GetResult();
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

      public async Task<RmiResponseDto> InterceptAsync(MethodInfo methodInfo, object[] methodArguments) {
         var result = await remoteServiceInvoker.Invoke(
            remoteServiceInfo,
            methodInfo,
            methodArguments).ConfigureAwait(false);
         return result;
      }
   }
}