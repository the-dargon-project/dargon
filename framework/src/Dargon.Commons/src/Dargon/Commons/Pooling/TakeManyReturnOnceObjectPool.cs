using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Dargon.Commons.Collections;

namespace Dargon.Commons.Pooling {
   public class ArenaListPool<T> {
      private readonly Func<T> factory;
      private readonly Action<T> zero;
      private readonly Action<T> reinit;
      
      private readonly ExposedArrayList<T> store = new ExposedArrayList<T>();
      private int ni;

      public ArenaListPool(Func<T> factory, Action<T> zero, Action<T> reinit) {
         this.factory = factory;
         this.zero = zero;
         this.reinit = reinit;
      }

      public T TakeObject() {
         if (store.Count == ni) {
            EnsurePoolSize(store.Count + 1);
         }

         var res = store[ni];
         ni++;
         return res;
      }

      private void EnsurePoolSize(int n) {
         // todo: No reason to expand multiple times instead of once
         while (store.Count < n) {
            var itemsToAdd = store.Count == 0 ? 4 : store.Count;
            var nextPoolSize = store.Count + itemsToAdd;

            store.EnsureCapacity(nextPoolSize);

            for (var i = 0; i < itemsToAdd; i++) {
               store.Add(factory());
            }
         }
      }

      public void TakeMany(int n, ExposedArrayList<T> target) {
         EnsurePoolSize(ni + n);
         target.EnsureCapacity(target.size + n);
         // Array.Copy(store.store, ni, target.store, target.size, n);

         for (var i = 0; i < n; i++) {
            target.store[target.size++] = store.store[ni++];
         }

         // target.size += n;
         // ni += n;
      }

      public void ReturnAll() {
         if (zero != null) {
            for (var i = 0; i < ni; i++) zero(store[i]);
         }

         // note: Moved reinit here instead of at take to improve
         // instruction/cache locality.
         //
         // At least in Terragami, this improves clip time 2ms
         // & otherwise there is a noticeable cost for reinit when
         // taking object. --miyu
         if (reinit != null) {
            for (var i = 0; i < ni; i++) reinit(store[i]);
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
#if DEBUG
         if (selectedPoolIndex == -1) throw new InvalidOperationException();
#endif
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

      public void TakeMany(int n, ExposedArrayList<T> target) {
         pools[selectedPoolIndex].TakeMany(n, target);
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
