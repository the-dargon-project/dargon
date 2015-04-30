using System;
using ItzWarty;
using ItzWarty.Collections;
using System.Linq;
using ItzWarty.Pooling;
using SCG = System.Collections.Generic;

namespace Dargon.Courier.Messaging {
   public class UnacknowledgedReliableMessageContainer {
      private readonly SCG.IReadOnlyDictionary<MessagePriority, int> kResendIntervalsByPriority = ImmutableDictionary.Of<MessagePriority, int>(
         MessagePriority.Low, 5,
         MessagePriority.Medium, 3,
         MessagePriority.High, 2
      );

      private readonly ObjectPool<UnacknowledgedReliableMessageContext> messageContextPool;
      private readonly IConcurrentQueue<UnacknowledgedReliableMessageContext> pendingQueue;
      private readonly IQueue<UnacknowledgedReliableMessageContext>[] resendQueues;
      private readonly IConcurrentDictionary<Guid, UnacknowledgedReliableMessageContext> unacknowledgedMessageContextsById;
      private int resendQueueIndex = 0;

      public UnacknowledgedReliableMessageContainer(ObjectPool<UnacknowledgedReliableMessageContext> messageContextPool) {
         this.messageContextPool = messageContextPool;

         pendingQueue = new ConcurrentQueue<UnacknowledgedReliableMessageContext>();
         resendQueues = Util.Generate<IQueue<UnacknowledgedReliableMessageContext>>(
            kResendIntervalsByPriority.Values.Max() + 1, 
            i => new Queue<UnacknowledgedReliableMessageContext>()
         );
         unacknowledgedMessageContextsById = new ConcurrentDictionary<Guid, UnacknowledgedReliableMessageContext>();
      }

      public void AddMessage(Guid messageId, Guid recipientId, object payload, MessagePriority messagePriority, MessageFlags messageFlags) {
         var messageContext = messageContextPool.TakeObject();
         messageContext.UpdateAndMarkUnacknowledged(messageId, recipientId, payload, messagePriority, messageFlags);
         unacknowledgedMessageContextsById.Add(messageId, messageContext);
         pendingQueue.Enqueue(messageContext);
      }

      public void HandleMessageAcknowledged(Guid recipientId, Guid messageId) {
         UnacknowledgedReliableMessageContext context;
         if (unacknowledgedMessageContextsById.TryGetValue(messageId, out context)) {
            if (context.RecipientId == recipientId) {
               context.MarkAcknowledged();
               unacknowledgedMessageContextsById.Remove(messageId.PairValue(context));
            }
         }
      }

      public void ProcessPendingQueues(Action<UnacknowledgedReliableMessageContext> unsentMessageProcessor) {
         UnacknowledgedReliableMessageContext context;
         while (pendingQueue.TryDequeue(out context)) {
            EnqueueMessageForResend(context);
         }

         var selectedQueueIndex = resendQueueIndex % resendQueues.Length;
         var currentQueue = resendQueues[selectedQueueIndex];
         while(currentQueue.Count > 0) {
            var messageContext = currentQueue.Dequeue();
            if (messageContext.Acknowledged) {
               messageContextPool.ReturnObject(messageContext);
            } else {
               unsentMessageProcessor(messageContext);
               EnqueueMessageForResend(messageContext);
            }
         }
         resendQueueIndex++;
      }

      public int GetUnsentMessagesRemaining () {
         return unacknowledgedMessageContextsById.Count;
      }

      private void EnqueueMessageForResend(UnacknowledgedReliableMessageContext context) {
         if (!context.Acknowledged) {
            var priorityOffset = kResendIntervalsByPriority[context.Priority];
            var targetQueueId = (resendQueueIndex + priorityOffset) % resendQueues.Length;
            resendQueues[targetQueueId].Enqueue(context);
         }
      }
   }
}
