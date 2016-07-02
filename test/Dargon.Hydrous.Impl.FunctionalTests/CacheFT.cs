using Dargon.Commons;
using Dargon.Courier;
using Dargon.Courier.TransportTier.Tcp;
using Dargon.Courier.TransportTier.Udp;
using Dargon.Hydrous.Impl;
using Dargon.Ryu;
using Nito.AsyncEx;
using NMockito;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.Collections;
using Dargon.Vox;
using Xunit;
using SCG = System.Collections.Generic;
using static Dargon.Commons.Channels.ChannelsExtensions;

namespace Dargon.Hydrous {
   public class CacheFT : NMockitoInstance {
      public async Task CustomProcessTestAsync() {
         await TaskEx.YieldToThreadPool();

         ThreadPool.SetMaxThreads(128, 128);

         var cohortCount = 4;
         var cluster = await CreateCluster<int, SetBox<int>>(cohortCount).ConfigureAwait(false);

         var workerCount = 100;
         var sync = new AsyncCountdownEvent(workerCount);
         var tasks = Util.Generate(
            workerCount,
            value => Go(async () => {
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

      [Fact]
      public async Task GetTestAsync() {
         await TaskEx.YieldToThreadPool();

         ThreadPool.SetMaxThreads(128, 128);

         var cohortCount = 4;
         var cluster = await CreateCluster<int, string>(cohortCount).ConfigureAwait(false);

         var workerCount = 100;
         var sync = new AsyncCountdownEvent(workerCount);
         var tasks = Util.Generate(
            workerCount,
            key => Go(async () => {
               try {
                  Console.WriteLine("Entered thread for worker of key " + key);

                  sync.Signal();
                  await sync.WaitAsync().ConfigureAwait(false);

                  const int kReadCount = 10;
                  for (var i = 0; i < kReadCount; i++) {
                     var node = cluster[i % cohortCount];
                     var entry = await node.UserCache.GetAsync(key).ConfigureAwait(false);
                     AssertEquals(key, entry.Key);
                     AssertEquals(null, entry.Value);
                     AssertFalse(entry.Exists);
                  }
               } catch (Exception e) {
                  Console.Error.WriteLine("Worker on key " + key + " threw " + e);
                  throw;
               }
            }));
         try {
            await Task.WhenAll(tasks).ConfigureAwait(false);
         } catch (Exception e) {
            Console.Error.WriteLine("Test threw " + e);
            throw;
         }
      }

      [Fact]
      public async Task PutTestAsync() {
         await TaskEx.YieldToThreadPool();

         ThreadPool.SetMaxThreads(128, 128);

         var cohortCount = 4;
         var cluster = await CreateCluster<int, string>(cohortCount).ConfigureAwait(false);

         var workerCount = 100;
         var sync = new AsyncCountdownEvent(workerCount);
         var tasks = Util.Generate(
            workerCount,
            key => Go(async () => {
               try {
                  Console.WriteLine("Entered thread for worker of key " + key);

                  sync.Signal();
                  await sync.WaitAsync().ConfigureAwait(false);

                  const int kWriteCount = 10;
                  for (var i = 0; i < kWriteCount; i++) {
                     var node = cluster[i % cohortCount];
                     var previousEntry = await node.UserCache.PutAsync(key, "value" + i).ConfigureAwait(false);
                     AssertEquals(key, previousEntry.Key);
                     if (i == 0) {
                        AssertNull(previousEntry.Value);
                        AssertFalse(previousEntry.Exists);
                     } else {
                        AssertEquals("value" + (i - 1), previousEntry.Value);
                        AssertTrue(previousEntry.Exists);
                     }
                  }
                  {
                     var node = cluster[kWriteCount % cohortCount];
                     var currentEntry = await node.UserCache.GetAsync(key).ConfigureAwait(false);
                     AssertEquals(key, currentEntry.Key);
                     AssertEquals("value" + (kWriteCount - 1), currentEntry.Value);
                     AssertEquals(true, currentEntry.Exists);
                  }
               } catch (Exception e) {
                  Console.Error.WriteLine("Worker on key " + key + " threw " + e);
                  throw;
               }
            }));
         try {
            await Task.WhenAll(tasks).ConfigureAwait(false);
         } catch (Exception e) {
            Console.Error.WriteLine("Test threw " + e);
            throw;
         }
      }

      private async Task<SCG.List<ICacheFacade<K, V>>> CreateCluster<K, V>(int cohortCount) {
         var cacheFacades = new SCG.List<ICacheFacade<K, V>>();
         for (var i = 0; i < cohortCount; i++) {
            cacheFacades.Add(await CreateCohortAsync<K, V>(i).ConfigureAwait(false));
         }
         return cacheFacades;
      }

      private async Task<ICacheFacade<K, V>> CreateCohortAsync<K, V>(int cohortNumber) {
         // Force loads assemblies in directory and registers to global serializer.
         new RyuFactory().Create();

         AssertTrue(cohortNumber >= 0 && cohortNumber < 15);
         var cohortId = Guid.Parse("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx".Replace('x', (cohortNumber + 1).ToString("x")[0]));
         var courier = await CourierBuilder.Create()
                                           .ForceIdentity(cohortId)
                                           .UseUdpMulticastTransport()
                                           .UseTcpServerTransport(21337 + cohortNumber)
                                           .BuildAsync().ConfigureAwait(false);
         var cacheInitializer = new CacheInitializer(courier);
         var myCacheFacade = cacheInitializer.CreateLocal<K, V>(
            new CacheConfiguration("my-cache"));
         return myCacheFacade;
      }

      public class CacheFTVoxTypes : VoxTypes {
         public CacheFTVoxTypes() : base(100000) {
            Register(0, typeof(SetBox<>));
            Register(12, typeof(SetAddEntryOperation<,>));
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