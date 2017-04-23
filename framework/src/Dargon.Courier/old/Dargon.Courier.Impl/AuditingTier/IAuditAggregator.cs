using System;
using Dargon.Commons.Collections;

namespace Dargon.Courier.AuditingTier {
   public interface IAuditAggregator<T> {
      void Put(T value);
   }

   public class NullAuditAggregator<T> : IAuditAggregator<T> {
      public void Put(T value) { }
   }

   public class DefaultAuditAggregator<T> : IAuditAggregator<T> {
      private readonly IConcurrentQueue<T> queue = new ConcurrentQueue<T>();

      public void Put(T value) {
         queue.Enqueue(value);
      }

      public AggregateStatistics<T> GetAndReset() {
         if (typeof(T) == typeof(int)) {
            return (AggregateStatistics<T>)(object)GetAndResetInt();
         } else if (typeof(T) == typeof(double)) {
            return (AggregateStatistics<T>)(object)GetAndResetDouble();
         } else {
            throw new NotImplementedException($"AuditAggregator doesn't support {typeof(T).Name}.");
         }
      }

      private AggregateStatistics<int> GetAndResetInt() {
         long sum = 0;
         int min = int.MaxValue;
         int max = 0;
         int count = 0;
         T val;
         bool noElements = true;
         while (queue.TryDequeue(out val)) {
            var value = (int)(object)val;
            sum += value;
            min = Math.Min(min, value);
            max = Math.Max(max, value);
            count++;
            noElements = false;
         }
         int average = count == 0 ? 0 : (int)(sum / count);
         if (noElements) {
            return null;
         }
         return new AggregateStatistics<int> {
            Sum = (int)sum,
            Min = min,
            Max = max,
            Count = count,
            Average = average
         };
      }

      private AggregateStatistics<double> GetAndResetDouble() {
         double sum = 0;
         double min = double.PositiveInfinity;
         double max = 0;
         int count = 0;
         T val;
         bool noElements = true;
         while (queue.TryDequeue(out val)) {
            var value = (double)(object)val;
            sum += value;
            min = Math.Min(min, value);
            max = Math.Max(max, value);
            count++;
            noElements = false;
         }
         double average = count == 0 ? 0 : sum / count;
         if (noElements) {
            return null;
         }
         return new AggregateStatistics<double> {
            Sum = (int)sum,
            Min = min,
            Max = max,
            Count = count,
            Average = average
         };
      }
   }
}