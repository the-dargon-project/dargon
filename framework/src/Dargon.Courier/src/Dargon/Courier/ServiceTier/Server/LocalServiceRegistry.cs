using Dargon.Commons.Utilities;
using Dargon.Courier.ServiceTier.Client;
using Dargon.Courier.ServiceTier.Exceptions;
using Dargon.Courier.ServiceTier.Vox;
using Dargon.Vox.Utilities;
using NLog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dargon.Commons.Collections;
using Dargon.Courier.Utilities;

namespace Dargon.Courier.ServiceTier.Server {
   public class LocalServiceRegistry {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      private readonly CopyOnAddDictionary<Guid, object> services = new CopyOnAddDictionary<Guid, object>();
      private readonly Identity identity;
      private readonly Messenger messenger;

      public LocalServiceRegistry(Identity identity, Messenger messenger) {
         this.identity = identity;
         this.messenger = messenger;
      }

      public void RegisterService(object service) {
         RegisterService(service.GetType(), service);
      }

      public void RegisterService(Type serviceType, object service) {
         Guid serviceId;
         if (!serviceType.TryGetInterfaceGuid(out serviceId)) {
            throw new InvalidOperationException($"Service of type {serviceType.FullName} does not have default service id.");
         }
         RegisterService(serviceId, serviceType, service);
      }

      public void RegisterService(Guid id, Type serviceType, object service) {
         var existingService = services.GetOrAdd(id, _ => service);
         if (existingService != service) {
            throw new InvalidOperationException($"Already have service registered for id {id}: {existingService}.");
         }
      }

      public async Task HandleInvocationRequestAsync(IInboundMessageEvent<RmiRequestDto> e) {
         Trace.Assert(e.Message.ReceiverId == identity.Id);

         if (logger.IsDebugEnabled) {
            logger.Debug($"Received RMI {e.Body.InvocationId.ToString("n").Substring(0, 6)} Request on method {e.Body.MethodName} for service {e.Body.ServiceId.ToString("n").Substring(0, 6)}");
         }

         var request = e.Body;
         if (!services.TryGetValue(request.ServiceId, out var service)) {
            if (logger.IsDebugEnabled) {
               logger.Debug($"Unable to handle RMI {e.Body.InvocationId.ToString("n").Substring(0, 6)} Request on method {e.Body.MethodName} for service {e.Body.ServiceId.ToString("n").Substring(0, 6)} - service not found.");
            }
            await RespondError(e, new ServiceUnavailableException(request)).ConfigureAwait(false);
            return;
         }

         var typeInfo = service.GetType().GetTypeInfo();
         var method = typeInfo.GetMethod(request.MethodName);
         if (method.IsGenericMethodDefinition) {
            method = method.MakeGenericMethod(request.MethodGenericArguments);
         }

         var args = request.MethodArguments;
         var parameters = method.GetParameters();
         for (var i = 0; i < args.Length; i++) {
            args[i] = VoxSerializationQuirks.CastToDesiredTypeIfIntegerLike(args[i], parameters[i].ParameterType);
         }

         object result;
         try {
            result = await TaskUtilities.UnboxValueIfTaskAsync(method.Invoke(service, args)).ConfigureAwait(false);
         } catch (Exception ex) {
            await RespondError(e, ex).ConfigureAwait(false);
            return;
         }

         var outResults = parameters.Where(p => p.IsOut).Select(p => args[p.Position]).ToArray();
         await RespondSuccess(e, outResults, result).ConfigureAwait(false);
      }

      private Task RespondSuccess(IInboundMessageEvent<RmiRequestDto> e, object[] outResults, object result) {
         if (logger.IsDebugEnabled) {
            logger.Debug($"Successfully handled RMI {e.Body.InvocationId.ToString("n").Substring(0, 6)} Request on method {e.Body.MethodName} for service {e.Body.ServiceId.ToString("n").Substring(0, 6)}. Sending return {result?.ToString() ?? "[null]"}.");
         }
         return messenger.SendReliableAsync(
            new RmiResponseDto {
               InvocationId = e.Body.InvocationId,
               ReturnValue = result,
               Outs = outResults,
               Exception = null,
            },
            e.Sender.Identity.Id);
      }

      private Task RespondError(IInboundMessageEvent<RmiRequestDto> e, Exception ex) {
         if (logger.IsDebugEnabled) {
            logger.Debug($"Threw when handling RMI {e.Body.InvocationId.ToString("n").Substring(0, 6)} Request on method {e.Body.MethodName} for service {e.Body.ServiceId.ToString("n").Substring(0, 6)}. Error: {ex}.");
         }
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
