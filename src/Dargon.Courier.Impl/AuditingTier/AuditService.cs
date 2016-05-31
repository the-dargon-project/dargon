using System;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.Collections;
using Dargon.Courier.AuditingTier.Utilities;

namespace Dargon.Courier.AuditingTier {
   public class AuditService {
      private const int kLogExpirationMillis = 30 * 60 * 1000;
      private const int kUpdateIntervalMillis = 1000;
      private const int kLogLength = kLogExpirationMillis / kUpdateIntervalMillis;

      private readonly ConcurrentDictionary<string, object> aggregatorsByName = new ConcurrentDictionary<string, object>();
      private readonly ConcurrentDictionary<string, AuditCounter> countersByName = new ConcurrentDictionary<string, AuditCounter>();
      private readonly ConcurrentDictionary<string, IDataPointCircularBuffer> dataSetCircularBuffersByName = new Commons.Collections.ConcurrentDictionary<string, IDataPointCircularBuffer>();
      private readonly ConcurrentSet<Action> updaterActions = new ConcurrentSet<Action>();

      private readonly CancellationToken shutdownCancellationToken;

      public AuditService(CancellationToken shutdownCancellationToken) {
         this.shutdownCancellationToken = shutdownCancellationToken;
      }

      public void Initialize() {
         RunAsync().Forget();
      }

      private async Task RunAsync() {
         try {
            while (!shutdownCancellationToken.IsCancellationRequested) {
               await Task.Delay(kUpdateIntervalMillis, shutdownCancellationToken);
               foreach (var action in updaterActions) {
                  action();
               }
            }
         } catch (OperationCanceledException) when (shutdownCancellationToken.IsCancellationRequested) { }
      }

      public AuditAggregator<T> GetAggregator<T>(string name) {
         return (AuditAggregator<T>)aggregatorsByName.GetOrAdd(name, CreateAggregator<T>);
      }

      private object CreateAggregator<T>(string name) {
         var aggregator = new AuditAggregator<T>();
         var dataSetCircularBuffer = new DataPointCircularBuffer<AggregateStatistics<T>>(kLogLength);
         dataSetCircularBuffersByName.AddOrThrow(name, dataSetCircularBuffer);
         updaterActions.TryAdd(() => {
            dataSetCircularBuffer.Put(aggregator.GetAndReset());
         });
         return aggregator;
      }

      public AuditCounter GetCounter(string name) {
         return countersByName.GetOrAdd(name, CreateCounter);
      }

      private AuditCounter CreateCounter(string arg) {
         var counter = new AuditCounter();
         var dataSetCircularBuffer = new DataPointCircularBuffer<int>(kLogLength);
         dataSetCircularBuffersByName.AddOrThrow(arg, dataSetCircularBuffer);
         updaterActions.TryAdd(() => {
            dataSetCircularBuffer.Put(counter.GetAndReset());
         });
         return counter;
      }

      public IDataPointCircularBuffer GetDataSetBuffer(string name) {
         return dataSetCircularBuffersByName[name];
      }

      public DataPointCircularBuffer<T> CreatePeriodicDataSet<T>(string name, Func<T> getValue) {
         var buffer = new DataPointCircularBuffer<T>(kLogLength);
         dataSetCircularBuffersByName.AddOrThrow(name, buffer);

         updaterActions.TryAdd(() => {
            buffer.Put(getValue());
         });

         return buffer;
      }
   }
}
