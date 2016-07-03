using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.Channels;
using Dargon.Commons.Collections;
using Dargon.Courier;
using Dargon.Vox;
using Nito.AsyncEx;
using NMockito;

namespace Dargon.Hydrous {
   public class CustomEntryOperationFT : NMockitoInstance {
      public async Task CustomProcessTestAsync() {
         await TaskEx.YieldToThreadPool();

         ThreadPool.SetMaxThreads(128, 128);

         var cohortCount = 4;
         var cluster = await TestUtils.CreateCluster<int, SetBox<int>>(cohortCount).ConfigureAwait(false);

         var workerCount = 100;
         var sync = new AsyncCountdownEvent(workerCount);
         var tasks = Util.Generate(
            workerCount,
            value => ChannelsExtensions.Go(async () => {
               sync.Signal();
               await sync.WaitAsync().ConfigureAwait(false);

               var node = cluster[value % cohortCount];
               var result = await node.UserCache.ProcessAsync(0, SetAddEntryOperation<int, int>.Create(value)).ConfigureAwait(false);
               AssertTrue(result);
            }));
         try {
            await Task.WhenAll(tasks).ConfigureAwait(false);

            Console.WriteLine("Validating set contents");
            var result = await cluster[0].UserCache.GetAsync(0).ConfigureAwait(false);
            AssertCollectionDeepEquals(new HashSet<int>(Enumerable.Range(0, 100)), result.Value.Set);
         } catch (Exception e) {
            Console.Error.WriteLine("Test threw " + e);
            throw;
         }
      }

      [AutoSerializable]
      public class SetBox<E> {
         public HashSet<E> Set { get; set; }
      }

      [AutoSerializable]
      public class SetAddEntryOperation<K, E> : IEntryOperation<K, SetBox<E>, bool> {
         public Guid Id { get; private set; }
         public EntryOperationType Type => EntryOperationType.ConditionalUpdate;

         public E Element { get; private set; }

         public Task<bool> ExecuteAsync(Entry<K, SetBox<E>> entry) {
            if (!entry.Exists) {
               entry.Value = new SetBox<E> { Set = new HashSet<E>() };
            }
            var result = entry.Value.Set.Add(Element);
            if (result) {
               entry.IsDirty = true;
            }
            Console.WriteLine(result + " " + entry.Value.Set.Join(", "));
            return Task.FromResult(result);
         }

         public static SetAddEntryOperation<K, E> Create(E element) => new SetAddEntryOperation<K, E> {
            Id = Guid.NewGuid(),
            Element = element
         };
      }
   }
}