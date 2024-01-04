using System.Threading;

namespace Dargon.Commons.Pooling;

public class Lease<T> {
   public Lease(IObjectPool<Lease<T>> pool, T item) {
      Pool = pool;
      Item = item;
   }

   public IObjectPool<Lease<T>> Pool { get; }
   public T Item { get; }
   public int ReferenceCount;

   public void Init(int initialRefCount = 1) => Interlocked.CompareExchange(ref ReferenceCount, initialRefCount, 0).AssertEquals(0);
   public void AddRef(int count) => Interlocked.Increment(ref ReferenceCount);
   public void AddRefs(int count) => Interlocked.Add(ref ReferenceCount, count);
   public void Release() {
      if (InterlockedMin.PreDecrement(ref ReferenceCount) == 0) {
         Pool.ReturnObject(this);
      }
   }
}

public static class Lease {
   public static Lease<T> Create<T>(IObjectPool<Lease<T>> pool, T item) => new Lease<T>(pool, item);
}