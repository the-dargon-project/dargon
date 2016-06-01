﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace Dargon.Commons.Pooling {
   public class DefaultObjectPool<T> : IObjectPool<T> {
      private readonly ThreadLocal<Stack<T>> container = new ThreadLocal<Stack<T>>(() => new Stack<T>(), false);
      private readonly Func<IObjectPool<T>, T> generator;
      private readonly string name;

      public DefaultObjectPool(Func<IObjectPool<T>, T> generator) : this(generator, null) {}
      public DefaultObjectPool(Func<IObjectPool<T>, T> generator, string name) {
         generator.ThrowIfNull("generator");
         
         this.generator = generator;
         this.name = name;
      }

      public string Name => name;

      public T TakeObject() {
         var s = container.Value;
         if (s.None()) {
            return generator(this);
         }
         return s.Pop();
      }

      public void ReturnObject(T item) {
         container.Value.Push(item);
      }
   }
}