using Dargon.Commons.Collections;
using Dargon.Commons.Exceptions;
using Dargon.Courier.AsyncPrimitives;
using Dargon.Courier.ServiceTier.Vox;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Dargon.Courier.ServiceTier.Client {
   public class RemoteServiceInvoker {
      private readonly ConcurrentDictionary<Guid, AsyncBox<RmiResponseDto>> responseBoxes = new ConcurrentDictionary<Guid,AsyncBox<RmiResponseDto>>();
      private readonly Messenger messenger;

      public RemoteServiceInvoker(Messenger messenger) {
         this.messenger = messenger;
      }

      public async Task HandleInvocationResponse(RmiResponseDto response) {
         await Task.Yield();

         AsyncBox<RmiResponseDto> responseBox;
         if (!responseBoxes.TryRemove(response.InvocationId, out responseBox)) {
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

         var responseBox = new AsyncBox<RmiResponseDto>();
         responseBoxes.AddOrThrow(invocationId, responseBox);

         await messenger.SendReliableAsync(request, serviceInfo.Peer.Identity.Id);

         // response box removed by HandleInvocationResponse - don't cleanup
         return await responseBox.GetResultAsync();
      }
   }
}