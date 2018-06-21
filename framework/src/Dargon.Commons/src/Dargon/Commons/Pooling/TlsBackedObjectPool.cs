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

      class X {
         public int Q { get; set; }
      }

      void Y() {
         var ctor = typeof(X).GetTypeInfo().DeclaredConstructors.First();
         var x = new X();
         ctor.Invoke(x, null);
         Console.WriteLine(ctor);
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
}