using System;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Hydrous;
using Dargon.Hydrous.Impl;
using Dargon.Hydrous.Impl.Diagnostics;
using Dargon.Hydrous.Impl.Store.Postgre;
using NMockito;
using static Dargon.Commons.Channels.ChannelsExtensions;

namespace Dargon.Hydrous {
   public class SomethingToDoWithEntryOperationChuggingAbstractBeanFactorySingletonTests : NMockitoInstance {
      public async Task RunAsync() {
         var cachePersistenceStrategy = new NullCachePersistenceStrategy<int, int>();
         var operationDiagnosticsTable = new NullOperationDiagnosticsTable();
         var keyCount = 100;
         var operationsPerKey = 50000;
         var singletons = Util.Generate(keyCount, i => new CacheRoot<int, int>.SomethingToDoWithEntryOperationChuggingAbstractBeanFactorySingleton(i, cachePersistenceStrategy, operationDiagnosticsTable));
         var readyLatch = new AsyncCountdownLatch(keyCount);
         var completionLatch = new AsyncCountdownLatch(keyCount);
         Console.WriteLine(DateTime.Now + " Begin Queue.");
         Util.Generate(keyCount, key => Go(async () => {
            await TaskEx.YieldToThreadPool();

            var executionContexts = Util.Generate(operationsPerKey, i => singletons[key].EnqueueOperationAndGetExecutionContext<int>(key, GetKeythPlus1000thFib.Create()));

            if (readyLatch.Signal()) {
               Console.WriteLine(DateTime.Now + " Go!");
            }
            await readyLatch.WaitAsync().ConfigureAwait(false);

            await singletons[key].InitializeAsync().ConfigureAwait(false);
            await Task.WhenAll(executionContexts.Map(ec => ec.ResultBox.GetResultAsync())).ConfigureAwait(false);
            completionLatch.Signal();
         }));
         await completionLatch.WaitAsync().ConfigureAwait(false);
         Console.WriteLine(DateTime.Now + " Done!");
      }

      /// <summary>
      /// Obviously this has no place in real entry operations, goal is to
      /// have arbitrary work that takes time.
      /// </summary>
      public class GetKeythPlus1000thFib : IEntryOperation<int, int, int> {
         public Guid Id { get; private set; }
         public EntryOperationType Type => EntryOperationType.Update;

         public Task<int> ExecuteAsync(Entry<int, int> entry) {
            return Task.FromResult(entry.Value++);

            var a = 0;
            var b = 1;
            for (var i = 0; i < entry.Key + 1000; i++) {
               var c = a + b;
               a = b;
               b = c;
            }
            entry.Value = b;
            return Task.FromResult(1);
         }

         public static GetKeythPlus1000thFib Create() => new GetKeythPlus1000thFib {
            Id = Guid.NewGuid()
         };
      }
   }
}
