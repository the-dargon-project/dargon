using System;
using System.Threading;

namespace Dargon.Commons.Pooling {
   public class TlsBufferManager<T> {
      private readonly Func<int, T[]> factory;
      private ThreadLocal<T[][]> tlsBuffers;
      private ThreadLocal<int> tlsNextBufferIndex;

      public TlsBufferManager(int reentrancy = 1) : this(i => new T[i], reentrancy) { }

      public TlsBufferManager(Func<int, T[]> factory, int reentrancy = 1) {
         this.factory = factory;
         this.tlsBuffers = new ThreadLocal<T[][]>(() => Arrays.Create(reentrancy, i => factory(0)));
         this.tlsNextBufferIndex = new ThreadLocal<int>();
      }

      public T[] Take(int size) {
         T[][] tlsbs = tlsBuffers.Value;

         if (tlsNextBufferIndex.Value >= tlsbs.Length) throw new InvalidOperationException();
         var i = tlsNextBufferIndex.Value++;

         if (tlsbs[i].Length < size) {
            checked {
               size = (int)BitMath.CeilingPow2((uint)size);
            }
            tlsbs[i] = factory(size);
         }
         return tlsbs[i];
      }

      public void Give(T[] buffer) {
         if (tlsNextBufferIndex.Value <= 0) throw new InvalidOperationException();
         if (tlsBuffers.Value[tlsNextBufferIndex.Value - 1] != buffer) throw new InvalidOperationException();
         tlsNextBufferIndex.Value--;
      }
   }

   public class AsyncLocalBufferManager<T> {
      private readonly Func<int, T[]> factory;
      private readonly int reentrancy;
      private AsyncLocal<T[][]> tlsBuffers;
      private AsyncLocal<int> tlsNextBufferIndex;

      public AsyncLocalBufferManager(int reentrancy = 1) : this(i => new T[i], reentrancy) { }

      public AsyncLocalBufferManager(Func<int, T[]> factory, int reentrancy = 1) {
         this.factory = factory;
         this.reentrancy = reentrancy;
         this.tlsBuffers = new AsyncLocal<T[][]>();
         this.tlsNextBufferIndex = new AsyncLocal<int>();
      }

      public T[] Take(int size) {
         if (tlsBuffers.Value == null) {
            tlsBuffers.Value = Arrays.Create(reentrancy, x => factory(0));
         }

         T[][] tlsbs = tlsBuffers.Value;

         if (tlsNextBufferIndex.Value >= tlsbs.Length) throw new InvalidOperationException();
         var i = tlsNextBufferIndex.Value++;

         if (tlsbs[i].Length < size) {
            checked {
               size = (int)BitMath.CeilingPow2((uint)size);
            }
            tlsbs[i] = factory(size);
         }
         return tlsbs[i];
      }

      public void Give(T[] buffer) {
         if (tlsNextBufferIndex.Value <= 0) throw new InvalidOperationException();
         if (tlsBuffers.Value[tlsNextBufferIndex.Value - 1] != buffer) throw new InvalidOperationException();
         tlsNextBufferIndex.Value--;
      }
   }
}
