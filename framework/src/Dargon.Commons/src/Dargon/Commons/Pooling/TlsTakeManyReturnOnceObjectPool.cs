namespace Dargon.Commons.Pooling;

public static class TlsTakeManyReturnOnceObjectPool {
   // reentrancy = 0 allows for a single level (no reentrancy)
   public static TlsTakeManyReturnOnceObjectPool<T> Create<T>(int reentrancy) where T : new() {
      var pools = Arrays.Create(reentrancy + 1, i => TlsBackedObjectPool.Create<T>());
      return new TlsTakeManyReturnOnceObjectPool<T>(pools);
   }

   public static TlsTakeManyReturnOnceObjectPool<T> CreateWithObjectZeroAndReconstruction<T>(int reentrancy) where T : new() {
      var pools = Arrays.Create(reentrancy + 1, i => TlsBackedObjectPool.CreateWithObjectZeroAndReconstruction<T>());
      return new TlsTakeManyReturnOnceObjectPool<T>(pools);
   }
}