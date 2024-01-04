namespace Dargon.Commons.Pooling;

public static class TlsBackedObjectPool {
   public static TlsBackedObjectPool<T> Create<T>() where T : new() => new TlsBackedObjectPool<T>(x => new T());
      
   public static TlsBackedObjectPool<T> CreateWithObjectZeroAndReconstruction<T>() where T : new() {
      return new TlsBackedObjectPool<T>(
         x => new T(),
         null,
         ReflectionUtils.Zero,
         ReflectionUtils.DefaultReconstruct);
   }
}