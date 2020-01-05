using System;
using System.Collections.Generic;

namespace Dargon.Commons.Pooling {
   public abstract class Pow2CollectionPoolBase<TCollection> {
      private readonly Func<int, IObjectPool<TCollection>> poolFactory;
      private readonly IObjectPool<TCollection>[] pools;

      public Pow2CollectionPoolBase(string name, Func<int, IObjectPool<TCollection>> poolFactory) {
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

      public TCollection Take(int minimumSize) {
         var (bin, actualSize) = FindBinAndActualSize(minimumSize);
         return pools[bin].TakeObject();
      }

      public void Return(TCollection collection) {
         var (bin, actualSize) = FindBinAndActualSize(Capacity(collection));
         if (actualSize != Capacity(collection)) throw new InvalidOperationException($"Collection length mismatch. Actual {Capacity(collection)} vs expected {actualSize}.");
         pools[bin].ReturnObject(collection);
      }

      protected abstract int Capacity(TCollection collection);
   }

   public class Pow2ArrayPool<T> : Pow2CollectionPoolBase<T[]> {
      public Pow2ArrayPool(string name, Func<int, IObjectPool<T[]>> poolFactory) : base(name, poolFactory) { }

      protected override int Capacity(T[] collection) => collection.Length;
   }

   public class Pow2ListPool<T> : Pow2CollectionPoolBase<List<T>> {
      public Pow2ListPool(string name, Func<int, IObjectPool<List<T>>> poolFactory) : base(name, poolFactory) { }

      protected override int Capacity(List<T> collection) => collection.Count;
   }
}
