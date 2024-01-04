using System;

namespace Dargon.Commons.Pooling {
   public static class ObjectPool {
      public static IObjectPool<T> CreateTlsBacked<T>(Func<T> generator) {
         return CreateTlsBacked<T>(pool => generator());
      }

      public static IObjectPool<T> CreateTlsBacked<T>(Func<IObjectPool<T>, T> generator) {
         return new TlsBackedObjectPool<T>(generator);
      }

      public static IObjectPool<T> CreateTlsBacked<T>(Func<T> generator, string name) {
         return CreateTlsBacked<T>(pool => generator(), name);
      }

      public static IObjectPool<T> CreateTlsBacked<T>(Func<IObjectPool<T>, T> generator, string name) {
         return new TlsBackedObjectPool<T>(generator, name, null, null);
      }

      public static IObjectPool<T> CreateSingleThreadedStackBacked<T>(Func<T> generator) {
         return CreateSingleThreadedStackBacked<T>(pool => generator());
      }

      public static IObjectPool<T> CreateSingleThreadedStackBacked<T>(Func<IObjectPool<T>, T> generator) {
         return new SingleThreadedStackBackedObjectPool<T>(generator);
      }

      public static IObjectPool<T> CreateSingleThreadedStackBacked<T>(Func<T> generator, string name) {
         return CreateSingleThreadedStackBacked<T>(pool => generator(), name);
      }

      public static IObjectPool<T> CreateSingleThreadedStackBacked<T>(Func<IObjectPool<T>, T> generator, string name) {
         return new SingleThreadedStackBackedObjectPool<T>(generator, name, null, null);
      }

      public static IObjectPool<T> CreateConcurrentQueueBacked<T>(Func<T> generator) {
         return CreateConcurrentQueueBacked<T>(pool => generator());
      }

      public static IObjectPool<T> CreateConcurrentQueueBacked<T>(Func<IObjectPool<T>, T> generator) {
         return new ConcurrentQueueBackedObjectPool<T>(generator);
      }

      public static IObjectPool<T> CreateConcurrentQueueBacked<T>(Func<T> generator, string name) {
         return CreateConcurrentQueueBacked<T>(pool => generator(), name);
      }

      public static IObjectPool<T> CreateConcurrentQueueBacked<T>(Func<IObjectPool<T>, T> generator, string name) {
         return new ConcurrentQueueBackedObjectPool<T>(generator, name);
      }
   }
}