using Dargon.Commons.Utilities;
using Dargon.Courier.ServiceTier.Client;
using Dargon.Courier.ServiceTier.Exceptions;
using Dargon.Courier.ServiceTier.Vox;
using Dargon.Vox.Utilities;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Dargon.Courier.ServiceTier.Server {
   public class LocalServiceRegistry {
      private readonly IncrementalDictionary<Guid, object> services = new IncrementalDictionary<Guid, object>();
      private readonly Messenger messenger;

      public LocalServiceRegistry(Messenger messenger) {
         this.messenger = messenger;
      }

      public void RegisterService(object service) {
         Guid serviceId;
         var serviceType = service.GetType();
         if (!serviceType.TryGetInterfaceGuid(out serviceId)) {
            throw new InvalidOperationException($"Service of type {serviceType.FullName} does not have default service id.");
         }
         RegisterService(serviceId, service);
      }

      public void RegisterService(Guid id, object service) {
         var existingService = services.GetOrAdd(id, _ => service);
         if (existingService != service) {
            throw new InvalidOperationException($"Already have service registered for id {id}: {existingService}.");
         }
      }

      public async Task HandleInvocationRequestAsync(InboundMessageEvent<RmiRequestDto> e) {
         var request = e.Body;
         object service;
         if (!services.TryGetValue(request.ServiceId, out service)) {
            await RespondError(e, new ServiceUnavailableException(request));
            return;
         }
         var typeInfo = service.GetType().GetTypeInfo();
         var method = typeInfo.GetMethod(request.MethodName);
         if (method.IsGenericMethodDefinition) {
            method = method.MakeGenericMethod(request.MethodGenericArguments);
         }
         object result;
         var args = request.MethodArguments;
         try {
            result = method.Invoke(service, args);
            var task = result as Task;
            if (task != null) {
               result = ((dynamic)task).Result;
            }
         } catch (Exception ex) {
            await RespondError(e, ex);
            return;
         }
         var outParameters = method.GetParameters().Where(p => p.IsOut);
         await RespondSuccess(e, outParameters.Select(p => args[p.Position]).ToArray(), result);
      }

      private Task RespondSuccess(InboundMessageEvent<RmiRequestDto> e, object[] outParameters, object result) {
         return messenger.SendReliableAsync(
            new RmiResponseDto {
               InvocationId = e.Body.InvocationId,
               ReturnValue = result,
               Outs = outParameters,
               Exception = null,
            },
            e.Sender.Identity.Id);
      }

      private Task RespondError(InboundMessageEvent<RmiRequestDto> e, Exception ex) {
         return messenger.SendReliableAsync(
            new RmiResponseDto {
               InvocationId = e.Body.InvocationId,
               Outs = new object[0],
               ReturnValue = null,
               Exception = RemoteException.Create(ex, e.Body)
            },
            e.Sender.Identity.Id);
      }
   }
}
