using System;
using System.Collections.Generic;
using System.Text;

namespace Dargon.Commons.Pooling {
   public class ArenaListPool<T> {
      private readonly Func<T> factory;
      private readonly Action<T> zero;
      private readonly Action<T> reinit;
      
      private readonly List<T> store = new List<T>();
      private int ni;

      public ArenaListPool(Func<T> factory, Action<T> zero, Action<T> reinit) {
         this.factory = factory;
         this.zero = zero;
         this.reinit = reinit;
      }

      public T TakeObject() {
         if (store.Count == ni) {
            var res = factory();
            store.Add(res);
            ni++;
            return res;
         } else {
            var res = store[ni];
            reinit(res);
            ni++;
            return res;
         }
      }

      public void ReturnAll() {
         if (zero != null) {
            for (var i = 0; i < ni; i++) zero(store[i]);
         }

         ni = 0;
      }
   }

   public class TakeManyReturnOnceObjectPool<T> {
      private readonly ArenaListPool<T>[] pools;
      private int selectedPoolIndex;

      public TakeManyReturnOnceObjectPool(ArenaListPool<T>[] pools) {
         this.pools = pools;
         this.selectedPoolIndex = -1;
      }

      public void EnterLevel() {
         if (selectedPoolIndex == pools.Length - 1)
            throw new InvalidOperationException();
         selectedPoolIndex++;
      }

      public T Take() {
         if (selectedPoolIndex == -1) throw new InvalidOperationException();
         var inst = pools[selectedPoolIndex].TakeObject();
         return inst;
      }

      public void LeaveLevelReturningTakenInstances() {
         if (selectedPoolIndex == -1) throw new InvalidOperationException();
         var pool = pools[selectedPoolIndex];
         pool.ReturnAll();
         selectedPoolIndex--;
      }

      public void LeaveAllLevelsReturningTakenInstances() {
         while (selectedPoolIndex >= 0) LeaveLevelReturningTakenInstances();
         if (selectedPoolIndex != -1) throw new InvalidOperationException();
      }
   }

   public static class TakeManyReturnOnceObjectPool {
      public static TakeManyReturnOnceObjectPool<T> Create<T>(int reentrancy) where T : new() {
         var pools = Arrays.Create(reentrancy + 1, i => new ArenaListPool<T>(() => new T(), null, null));
         return new TakeManyReturnOnceObjectPool<T>(pools);
      }

      public static TakeManyReturnOnceObjectPool<T> CreateWithObjectZeroAndReconstruction<T>(int reentrancy, Action<T> zeroOverride = null, Action<T> reinitOverride = null) where T : new() {
         var pools = Arrays.Create(reentrancy + 1, i => new ArenaListPool<T>(() => new T(), zeroOverride ?? ReflectionUtils.Zero, reinitOverride ?? ReflectionUtils.DefaultReconstruct));
         return new TakeManyReturnOnceObjectPool<T>(pools);
      }
   }
}
