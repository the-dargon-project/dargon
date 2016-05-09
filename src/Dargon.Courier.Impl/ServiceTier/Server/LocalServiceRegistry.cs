using Dargon.Courier.ServiceTier.Exceptions;
using Dargon.Courier.ServiceTier.Vox;
using Dargon.Vox.Utilities;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Dargon.Courier.ServiceTier.Client;

namespace Dargon.Courier.ServiceTier.Server {
   public class LocalServiceRegistry {
      private readonly IncrementalDictionary<Guid, object> services = new IncrementalDictionary<Guid, object>();
      private readonly Messenger messenger;

      public LocalServiceRegistry(Messenger messenger) {
         this.messenger = messenger;
      }

      public async Task HandleInvocationRequestAsync(InboundMessageEvent<RmiRequestDto> e) {
         var request = e.Body;
         object service;
         if (services.TryGetValue(request.ServiceId, out service)) {
            await RespondError(e, new ServiceUnavailableException(request));
            return;
         }
         var typeInfo = service.GetType().GetTypeInfo();
         var method = typeInfo.GetMethod(request.MethodName);
         if (method.IsGenericMethodDefinition) {
            method = method.MakeGenericMethod(request.MethodGenericArguments);
         }
         object result;
         try {
            result = method.Invoke(service, request.MethodArguments);
            var task = result as Task;
            if (task != null) {
               result = ((dynamic)task).Result;
            }
         } catch (Exception ex) {
            await RespondError(e, ex);
            return;
         }
         await RespondSuccess(e, result);
      }

      private Task RespondSuccess(InboundMessageEvent<RmiRequestDto> e, object result) {
         return messenger.SendReliableAsync(
            new RmiResponseDto {
               InvocationId = e.Body.InvocationId,
               ReturnValue = result
            },
            e.Sender.Identity.Id);
      }

      private Task RespondError(InboundMessageEvent<RmiRequestDto> e, Exception ex) {
         return messenger.SendReliableAsync(
            new RmiResponseDto {
               InvocationId = e.Body.InvocationId,
               Exception = RemoteException.Create(ex, e.Body)
            },
            e.Sender.Identity.Id);
      }
   }
}
