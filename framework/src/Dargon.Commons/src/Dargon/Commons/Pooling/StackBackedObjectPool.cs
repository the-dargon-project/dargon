using System;
using System.Collections.Generic;

namespace Dargon.Commons.Pooling {
   public class SingleThreadedStackBackedObjectPool<T> : IObjectPool<T> {
      private readonly Stack<T> container = new Stack<T>();
      private readonly Func<IObjectPool<T>, T> generator;
      private readonly string name;

      public SingleThreadedStackBackedObjectPool(Func<IObjectPool<T>, T> generator) : this(generator, null) { }
      public SingleThreadedStackBackedObjectPool(Func<IObjectPool<T>, T> generator, string name) {
         generator.ThrowIfNull("generator");

         this.generator = generator;
         this.name = name;
      }

      public string Name => name;
      public int Count => container.Count;

      public T TakeObject() => container.Count == 0 ? generator(this) : container.Pop();

      public void ReturnObject(T item) => container.Push(item);
   }
}