namespace Dargon.Commons.Pooling;

public static class SingleThreadedStackBackedObjectPool {
   public static SingleThreadedStackBackedObjectPool<T> Create<T>() where T : new()
      => new SingleThreadedStackBackedObjectPool<T>(x => new T());
      
   public static SingleThreadedStackBackedObjectPool<T> CreateWithObjectZeroAndReconstruction<T>() where T : new() {
      return new SingleThreadedStackBackedObjectPool<T>(
         x => new T(),
         null,
         ReflectionUtils.Zero,
         ReflectionUtils.DefaultReconstruct);
   }
}