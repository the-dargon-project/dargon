using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.PubSubTier.Vox;

namespace Dargon.Courier.PubSubTier.Publishers {
   internal interface IInternalPublisherInterfaceConstraint {
      Task CreateLocalTopicAsync(Guid guid);
      Task DestroyLocalTopicAsync(Guid guid);
      Task PublishToLocalTopicAsync<T>(Guid guid, T payload);
   }

   public class Publisher : IInternalPublisherInterfaceConstraint {
      private readonly Messenger messenger;
      private readonly LocalTopicsTable localTopicsTable;

      public Publisher(Messenger messenger, LocalTopicsTable localTopicsTable) {
         this.messenger = messenger;
         this.localTopicsTable = localTopicsTable;
      }

      public async Task CreateLocalTopicAsync(Guid guid) => await localTopicsTable.AddTopicAsync(guid);

      public async Task DestroyLocalTopicAsync(Guid guid) => await localTopicsTable.RemoveTopicAsync(guid);

      public async Task PublishToLocalTopicAsync<T>(Guid guid, T payload) {
         var topicContext = await localTopicsTable.QueryLocalTopicContextAsync(guid);
         var isReliable = topicContext.IsReliable;
         var subs = await topicContext.QuerySubscriptionsAsync();
         var publishes = new Task[subs.Count];
         var ni = 0;
         foreach (var sub in subs) {
            publishes[ni++] = messenger.SendAsync(new PubSubNotification {
               Topic = guid,
               Seq = topicContext.GetNextSequenceNumber(),
               Payload = payload,
            }, sub, isReliable);
         }
         await Task.WhenAll(publishes);
      }
   }
}
