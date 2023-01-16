using System;
using System.IO;
using System.Threading;
using Dargon.Commons.IO;

namespace Dargon.Commons.Pooling;

public class LeasedBufferView {
   private int __refcount;

   public LeasedBufferView(IObjectPool<LeasedBufferView> pool, int capacity) {
      Pool = pool;
      RawBuffer = new byte[capacity];
   }

   public readonly IObjectPool<LeasedBufferView> Pool;
   public readonly byte[] RawBuffer;
   public int Offset;
   public int Capacity => RawBuffer.Length;
   public int Length;
   public Memory<byte> Memory => RawBuffer.AsMemory(Offset, Length);
   public Span<byte> Span => RawBuffer.AsSpan(Offset, Length);
   public int ReferenceCount => __refcount;
   public object Tag;
   public MemoryByteStream CreateStream() => new(Memory);

   public void Init(int initialRefCount = 1) {
      Interlocked2.Read(ref __refcount).AssertEquals(0);
      Interlocked.CompareExchange(ref __refcount, initialRefCount, 0).AssertEquals(0);
   }

   public void SetDataRange(int offset, int length) {
      Offset = offset;
      Length = length;
   }

   /// <summary>
   /// Helper property for indicating handle transfer vs sharing semantics.
   /// Share indicates the LBV is being shared rather than transferred ownership, so an AddRef happens.
   /// </summary>
   public LeasedBufferView Share {
      get {
         AddRef();
         return this;
      }
   }

   /// <summary>
   /// Helper property for indicating handle transfer vs sharing semantics.
   /// Transfer indicates the LBV's ownership is being transferred; the LBV should not have its counter decremented.
   /// </summary>
   public LeasedBufferView Transfer => this;

   public void AddRef() => Interlocked.Increment(ref __refcount);
   public void AddRefs(int count) => Interlocked.Add(ref __refcount, count);
   public void Release() {
      if (Interlocked2.PreDecrement(ref __refcount) == 0) {
         Pool.ReturnObject(this);
      }
   }
}