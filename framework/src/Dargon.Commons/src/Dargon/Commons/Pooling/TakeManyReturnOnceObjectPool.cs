using System;

namespace Dargon.Commons.Pooling;

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