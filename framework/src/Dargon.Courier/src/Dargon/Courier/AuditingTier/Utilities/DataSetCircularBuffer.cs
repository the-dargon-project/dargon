using System;
using System.Collections.Concurrent;
using Dargon.Commons.Exceptions;
using Dargon.Courier.ManagementTier.Vox;

namespace Dargon.Courier.AuditingTier.Utilities {
   public interface IDataPointCircularBuffer {
      Type ElementType { get; }
   }

   public class DataPointCircularBuffer<T> : IDataPointCircularBuffer {
      private readonly ConcurrentQueue<DataPoint<T>> dataPoints = new ConcurrentQueue<DataPoint<T>>();
      private int maxElementCount;

      public DataPointCircularBuffer(int maxElementCount) {
         this.maxElementCount = maxElementCount;
      }

      public Type ElementType => typeof(T);

      public void Put(T value) {
         var point = new DataPoint<T> {
            Time = DateTime.Now,
            Value = value
         };
         dataPoints.Enqueue(point);
         if (dataPoints.Count > maxElementCount) {
            DataPoint<T> throwaway;
            if (!dataPoints.TryDequeue(out throwaway)) {
               throw new InvalidStateException();
            }
         }
      }

      public DataPoint<T>[] ToArray() {
         return dataPoints.ToArray();
      }
   }
}