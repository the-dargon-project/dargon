using Dargon.Commons.Collections;
using Dargon.Commons.Exceptions;
using static Dargon.Commons.Channels.ChannelsExtensions;
using Dargon.Courier.ServiceTier.Vox;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
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
         Trace.Assert(x.Message.ReceiverId == localIdentity.Id);
         await TaskEx.YieldToThreadPool();

         var response = x.Body;
         logger.Info($"Handling invocation response for {response.InvocationId}.");
         AsyncBox<RmiResponseDto> responseBox;
         if (!responseBoxes.TryRemove(response.InvocationId, out responseBox)) {
            logger.Error($"Could not find response box for invocation id {response.InvocationId}.");
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
            logger.Error($"Swallowing as routed to self - RMI {invocationId.ToString("n").Substring(0, 6)} Request on method {methodInfo.Name} for service {serviceInfo.ServiceType.Name}. Local: {localIdentity.Id.ToString("n").Substring(0, 6)}, Remote: {serviceInfo.Peer.Identity.Id.ToString("n").Substring(0, 6)}");
            throw new ArgumentException("Attempted to perform remote service invocation on self.");
         }

         logger.Debug("____A1");
         var responseBox = new AsyncBox<RmiResponseDto>();
         responseBoxes.AddOrThrow(invocationId, responseBox);

         logger.Debug("____A2");

         await messenger.SendReliableAsync(request, serviceInfo.Peer.Identity.Id).ConfigureAwait(false);
         logger.Debug("____A3");

         logger.Debug($"Sent RMI {invocationId.ToString("n").Substring(0, 6)} Request on method {methodInfo.Name} for service {serviceInfo.ServiceType.Name}");

         // response box removed by HandleInvocationResponse - don't cleanup
         var result = await responseBox.GetResultAsync().ConfigureAwait(false);

         logger.Debug($"Received RMI {invocationId.ToString("n").Substring(0, 6)} Response on method {methodInfo.Name} for service {serviceInfo.ServiceType.Name}");

         return result;
      }
   }
}