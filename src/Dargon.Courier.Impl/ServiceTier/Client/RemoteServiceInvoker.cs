using Dargon.Commons.Collections;
using Dargon.Commons.Exceptions;
using Dargon.Courier.AsyncPrimitives;
using Dargon.Courier.ServiceTier.Vox;
using System;
using System.Reflection;
using System.Threading.Tasks;
using NLog;

namespace Dargon.Courier.ServiceTier.Client {
   public class RemoteServiceInvoker {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      private readonly ConcurrentDictionary<Guid, AsyncBox<RmiResponseDto>> responseBoxes = new ConcurrentDictionary<Guid,AsyncBox<RmiResponseDto>>();
      private readonly Identity localIdentity;
      private readonly Messenger messenger;

      public RemoteServiceInvoker(Identity localIdentity, Messenger messenger) {
         this.localIdentity = localIdentity;
         this.messenger = messenger;
      }

      public async Task HandleInvocationResponse(IInboundMessageEvent<RmiResponseDto> x) {
         await Task.Yield();

         var response = x.Body;
         logger.Warn($"Handling invocation response for {response.InvocationId}.");
         AsyncBox<RmiResponseDto> responseBox;
         if (!responseBoxes.TryRemove(response.InvocationId, out responseBox)) {
            logger.Warn($"Could not find response box for invocation id {response.InvocationId}.");
            throw new InvalidStateException();
         }
         responseBox.SetResult(response);
      }

      public async Task<RmiResponseDto> Invoke(RemoveServiceInfo serviceInfo, MethodInfo methodInfo, object[] methodArguments) {
         var invocationId = Guid.NewGuid();
         var request = new RmiRequestDto {
            InvocationId = invocationId,
            MethodArguments = methodArguments,
            MethodGenericArguments = methodInfo.GetGenericArguments(),
            MethodName = methodInfo.Name,
            ServiceId = serviceInfo.ServiceId
         };

         logger.Debug($"Sending RMI {invocationId.ToString("n").Substring(0, 6)} Request on method {methodInfo.Name} for service {serviceInfo.ServiceType.Name}. Local: {localIdentity.Id.ToString("n").Substring(0, 6)}, Remote: {serviceInfo.Peer.Identity.Id.ToString("n").Substring(0, 6)}");

         if (localIdentity.Id == serviceInfo.Peer.Identity.Id) {
            throw new ArgumentException("Attempted to perform remote service invocation on self.");
         }

         var responseBox = new AsyncBox<RmiResponseDto>();
         responseBoxes.AddOrThrow(invocationId, responseBox);

         await messenger.SendReliableAsync(request, serviceInfo.Peer.Identity.Id).ConfigureAwait(false);

         logger.Debug($"Sent RMI {invocationId.ToString("n").Substring(0, 6)} Request on method {methodInfo.Name} for service {serviceInfo.ServiceType.Name}");

         // response box removed by HandleInvocationResponse - don't cleanup
         var result = await responseBox.GetResultAsync().ConfigureAwait(false);

         logger.Debug($"Received RMI {invocationId.ToString("n").Substring(0, 6)} Response on method {methodInfo.Name} for service {serviceInfo.ServiceType.Name}");

         return result;
      }
   }
}