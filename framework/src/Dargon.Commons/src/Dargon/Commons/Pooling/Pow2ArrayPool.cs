using System;

namespace Dargon.Commons.Pooling {
   public class Pow2ArrayPool<T> {
      private readonly Func<int, IObjectPool<T[]>> poolFactory;
      private readonly IObjectPool<T[]>[] pools;

      public Pow2ArrayPool(string name, Func<int, IObjectPool<T[]>> poolFactory) {
         Name = name;
         this.poolFactory = poolFactory;
         pools = Arrays.Create(32, i => poolFactory(i == 0 ? 0 : 1 << (i - 1)));
      }

      public string Name { get; }

      private (int bin, int actualSize) FindBinAndActualSize(int minimumSize) {
         if (minimumSize < 0) throw new ArgumentOutOfRangeException();
         if (minimumSize == 0) {
            return (0, 0);
         }

         var actualSize = 1;
         var bin = 1;
         while (actualSize < minimumSize) {
            actualSize <<= 1;
            bin++;
         }

         return (bin, actualSize);
      }

      public T[] Take(int minimumSize) {
         var (bin, actualSize) = FindBinAndActualSize(minimumSize);
         return pools[bin].TakeObject();
      }

      public void Return(T[] arr) {
         var (bin, actualSize) = FindBinAndActualSize(arr.Length);
         if (actualSize != arr.Length) throw new InvalidOperationException($"Array length mismatch. Actual {arr.Length} vs expected {actualSize}.");
         pools[bin].ReturnObject(arr);
      }
   }
}
