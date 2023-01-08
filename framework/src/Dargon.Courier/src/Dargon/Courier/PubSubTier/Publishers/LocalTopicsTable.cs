using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Courier.PeeringTier;

namespace Dargon.Courier.PubSubTier.Publishers {
   public class LocalTopicsTable {
      /// <summary>
      /// immutable hashsets - keys are replaced on subscription changes 
      /// </summary>
      private readonly ConcurrentDictionary<Guid, LocalTopicContext> topicIdToSubscriptions = new();
      private readonly AsyncReaderWriterLock sync = new AsyncReaderWriterLock();

      public async Task AddTopicAsync(Guid topicId) {
         await using var mut = await sync.CreateWriterGuardAsync();
         topicIdToSubscriptions.AddOrThrow(topicId, new LocalTopicContext());
      }

      public async Task RemoveTopicAsync(Guid topicId) {
         await using var mut = await sync.CreateWriterGuardAsync();
         var success = topicIdToSubscriptions.TryRemove(topicId, out var context);
         if (!success) throw new KeyNotFoundException();
      }

      public async Task AddSubscriptionAsync(Guid topicId, PeerContext peer) {
         await using var mut = await sync.CreateWriterGuardAsync();
         var topicContext = topicIdToSubscriptions[topicId];
         await topicContext.AddSubscriptionAsync(peer);
      }

      public async Task RemoveSubscriptionAsync(Guid topicId, PeerContext peer) {
         await using var mut = await sync.CreateWriterGuardAsync();
         var topicContext = topicIdToSubscriptions[topicId];
         await topicContext.RemoveSubscriptionAsync(peer);
      }

      public async Task<LocalTopicContext> QueryLocalTopicContextAsync(Guid topicId) {
         await using var mut = await sync.CreateReaderGuardAsync();
         return topicIdToSubscriptions[topicId];
      }
   }

   public class LocalTopicContext {
      private uint nextSequenceNumber = 0;

      // Below max val to accommodate multiple threads that need to throw.
      private uint kOverflowSeqRange = 0xFF000000U;

      private readonly AsyncReaderWriterLock sync = new AsyncReaderWriterLock();
      private HashSet<PeerContext> subs = new HashSet<PeerContext>();
      public bool IsReliable => true;

      public async Task AddSubscriptionAsync(PeerContext peer) {
         await using var mut = await sync.CreateWriterGuardAsync();
         var newSet = new HashSet<PeerContext>(subs);
         newSet.Add(peer).AssertIsTrue();
         subs = newSet;
      }

      public async Task RemoveSubscriptionAsync(PeerContext peer) {
         await using var mut = await sync.CreateWriterGuardAsync();
         var newSet = new HashSet<PeerContext>(subs);
         newSet.Remove(peer).AssertIsTrue();
         subs = newSet;
      }

      public async Task<HashSet<PeerContext>> QuerySubscriptionsAsync() {
         await using var mut = await sync.CreateReaderGuardAsync();
         return subs;
      }

      public uint GetNextSequenceNumber() {
         // increment returns the value after a bump, so subtract 1
         var seq = Interlocked.Increment(ref nextSequenceNumber) - 1;
         if (seq > kOverflowSeqRange) {
            throw new OverflowException();
         }
         return seq;
      }
   }
}
