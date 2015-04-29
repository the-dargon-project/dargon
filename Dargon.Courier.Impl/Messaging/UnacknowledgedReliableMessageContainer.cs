using System;
using ItzWarty;
using ItzWarty.Collections;
using System.Linq;
using SCG = System.Collections.Generic;

namespace Dargon.Courier.Messaging {
   public class UnacknowledgedReliableMessageContainer {
      private readonly SCG.IReadOnlyDictionary<MessagePriority, int> kResendIntervalsByPriority = ImmutableDictionary.Of<MessagePriority, int>(
         MessagePriority.Low, 5,
         MessagePriority.Medium, 3,
         MessagePriority.High, 2
      );

      private readonly IConcurrentQueue<UnacknowledgedReliableMessageContext> pendingQueue;
      private readonly IQueue<UnacknowledgedReliableMessageContext>[] resendQueues;
      private readonly IConcurrentDictionary<Guid, UnacknowledgedReliableMessageContext> messageContextsById;
      private int resendQueueIndex = 0;

      public UnacknowledgedReliableMessageContainer() {
         pendingQueue = new ConcurrentQueue<UnacknowledgedReliableMessageContext>();
         resendQueues = Util.Generate<IQueue<UnacknowledgedReliableMessageContext>>(
            kResendIntervalsByPriority.Values.Max() + 1, 
            i => new Queue<UnacknowledgedReliableMessageContext>()
         );
         messageContextsById = new ConcurrentDictionary<Guid, UnacknowledgedReliableMessageContext>();
      }

      public void AddMessage(Guid messageId, Guid recipientId, object payload, MessagePriority messagePriority, MessageFlags messageFlags) {
         var messageContext = new UnacknowledgedReliableMessageContext(messageId, recipientId, payload, messagePriority, messageFlags);
         messageContextsById.Add(messageId, messageContext);
         pendingQueue.Enqueue(messageContext);
      }

      public void HandleMessageAcknowledged(Guid recipientId, Guid messageId) {
         UnacknowledgedReliableMessageContext context;
         if (messageContextsById.TryGetValue(messageId, out context)) {
            if (context.RecipientId == recipientId) {
               context.MarkAcknowledged();
               messageContextsById.Remove(messageId.PairValue(context));
            }
         }
      }

      public SCG.IEnumerable<UnacknowledgedReliableMessageContext> ProcessPendingQueuesAndGetNextMessagesToSend() {
         UnacknowledgedReliableMessageContext context;
         while (pendingQueue.TryDequeue(out context)) {
            EnqueueMessageForResendIfUnacknowledged(context);
         }

         var selectedQueueIndex = resendQueueIndex % resendQueues.Length;
         var currentQueue = resendQueues[selectedQueueIndex];
         resendQueues[selectedQueueIndex] = new Queue<UnacknowledgedReliableMessageContext>();
         currentQueue.ForEach(EnqueueMessageForResendIfUnacknowledged);
         resendQueueIndex++;
         return currentQueue;
      }

      public int GetUnsentMessagesRemaining () {
         return messageContextsById.Count;
      }

      private void EnqueueMessageForResendIfUnacknowledged(UnacknowledgedReliableMessageContext context) {
         if (!context.Acknowledged) {
            var priorityOffset = kResendIntervalsByPriority[context.Priority];
            var targetQueueId = (resendQueueIndex + priorityOffset) % resendQueues.Length;
            resendQueues[targetQueueId].Enqueue(context);
         }
      }
   }
}
