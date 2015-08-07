using System.Threading.Tasks;
using ItzWarty.Threading;

namespace Dargon.Courier.Messaging {
   public class PeriodicResenderImpl {
      private readonly IThreadingProxy threadingProxy;
      private readonly UnacknowledgedReliableMessageContainer unacknowledgedReliableMessageContainer;
      private readonly MessageTransmitterImpl messageTransmitter;
      private readonly ICancellationTokenSource cancellationTokenSource;
      private Task mainLoopTask;

      public PeriodicResenderImpl(IThreadingProxy threadingProxy, UnacknowledgedReliableMessageContainer unacknowledgedReliableMessageContainer, MessageTransmitterImpl messageTransmitter) {
         this.threadingProxy = threadingProxy;
         this.unacknowledgedReliableMessageContainer = unacknowledgedReliableMessageContainer;
         this.messageTransmitter = messageTransmitter;

         this.cancellationTokenSource = threadingProxy.CreateCancellationTokenSource();
      }

      public void Start() {
         this.mainLoopTask = MainLoopAsync();
      }

      private async Task MainLoopAsync() {
         var cancellationToken = cancellationTokenSource.Token;
         while (!cancellationToken.IsCancellationRequested) {
            unacknowledgedReliableMessageContainer.ProcessPendingQueues(
               (message) => messageTransmitter.Transmit(
                  message.MessageId, 
                  message.RecipientId, 
                  message.Payload, 
                  message.Flags
               )
            );

            await Task.Delay(CourierPeriodicsConstants.kResendTickIntervalMillis);
         }
      }
   }

   public static class CourierPeriodicsConstants {
      public const int kResendTickIntervalMillis = 100;
   }
}