using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Dargon.Commons.Pooling {
   public class SingleThreadedStackBackedObjectPool<T> : IObjectPool<T> {
      private readonly Stack<T> container = new Stack<T>();
      private readonly Func<IObjectPool<T>, T> generator;
      private readonly string name;
      private readonly Action<T> zero;
      private readonly Action<T> reinit;

      public SingleThreadedStackBackedObjectPool(Func<IObjectPool<T>, T> generator) : this(generator, null, null, null) { }

      public SingleThreadedStackBackedObjectPool(Func<IObjectPool<T>, T> generator, string name = null, Action<T> zero = null, Action<T> reinit = null) {
         generator.ThrowIfNull("generator");

         this.generator = generator;
         this.name = name;
         this.zero = zero;
         this.reinit = reinit;
      }

      public string Name => name;
      public int Count => container.Count;

      public T TakeObject() {
         if (container.Count == 0) {
            return generator(this);
         }
         
         var item = container.Pop();
         reinit?.Invoke(item);
         return item;
      }

      public void ReturnObject(T item) {
         zero?.Invoke(item);
         container.Push(item);
      }
   }
}