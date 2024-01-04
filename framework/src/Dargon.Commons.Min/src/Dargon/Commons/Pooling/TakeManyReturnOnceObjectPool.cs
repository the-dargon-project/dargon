using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Dargon.Commons.Collections;

namespace Dargon.Commons.Pooling {
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

      public void TakeMany(int n, ExposedArrayListMin<T> target) {
         pools[selectedPoolIndex].TakeMany(n, target);
      }
   }
}
