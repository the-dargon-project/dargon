using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Dargon.Commons.Pooling {
   public class TlsBackedObjectPool<T> : IObjectPool<T> {
      private readonly ThreadLocal<Stack<T>> container = new ThreadLocal<Stack<T>>(() => new Stack<T>(), false);
      private readonly Func<IObjectPool<T>, T> generator;
      private readonly string name;
      private readonly Action<T> zero;
      private readonly Action<T> reinit;

      public TlsBackedObjectPool(Func<IObjectPool<T>, T> generator) : this(generator, null, null, null) {}

      public TlsBackedObjectPool(Func<IObjectPool<T>, T> generator, string name, Action<T> zero, Action<T> reinit) {
         generator.ThrowIfNull("generator");
         
         this.generator = generator;
         this.name = name;
         this.zero = zero;
         this.reinit = reinit;
      }

      public string Name => name;
      public int Count => container.Value.Count;

      public T TakeObject() {
         var s = container.Value;
         if (s.Count == 0) return generator(this);
         else {
            var item = s.Pop();
            reinit?.Invoke(item);
            return item;
         }
      }

      public void ReturnObject(T item) {
         zero?.Invoke(item);
         container.Value.Push(item);
      }

      // Useful for retrofitting old code that does wasteful new[]s.
      public T UnsafeTakeAndGive() {
         if (zero != null) throw new InvalidOperationException();
         var o = TakeObject();
         ReturnObject(o);
         return o;
      }
   }

   public class TlsTakeManyReturnOnceObjectPool<T> {
      private readonly TlsBackedObjectPool<T>[] tlsPools;
      private readonly ThreadLocal<List<T>[]> tlsTakenInstancesByPoolIndex;
      private readonly ThreadLocal<int> tlsSelectedPoolIndex;

      public TlsTakeManyReturnOnceObjectPool(TlsBackedObjectPool<T>[] pools) {
         this.tlsPools = pools;
         this.tlsTakenInstancesByPoolIndex = new ThreadLocal<List<T>[]>(() => pools.Map(p => new List<T>()));
         this.tlsSelectedPoolIndex = new ThreadLocal<int>(() => -1);
      }

      public void EnterLevel() {
         if (tlsSelectedPoolIndex.Value == tlsPools.Length - 1) throw new InvalidOperationException();
         tlsSelectedPoolIndex.Value++;
      }

      public T Take() {
         if (tlsSelectedPoolIndex.Value == -1) throw new InvalidOperationException();
         var inst = tlsPools[tlsSelectedPoolIndex.Value].TakeObject();
         tlsTakenInstancesByPoolIndex.Value[tlsSelectedPoolIndex.Value].Add(inst);
         return inst;
      }

      public void LeaveLevelReturningTakenInstances() {
         if (tlsSelectedPoolIndex.Value == -1) throw new InvalidOperationException();
         var takenInstances = tlsTakenInstancesByPoolIndex.Value[tlsSelectedPoolIndex.Value];
         var pool = tlsPools[tlsSelectedPoolIndex.Value];
         tlsSelectedPoolIndex.Value--;
         foreach (var inst in takenInstances) {
            pool.ReturnObject(inst);
         }
         takenInstances.Clear();
      }
   }
}