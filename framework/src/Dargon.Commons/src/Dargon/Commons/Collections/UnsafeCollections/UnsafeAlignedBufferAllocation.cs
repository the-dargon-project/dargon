using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dargon.Commons.Collections.UnsafeCollections;

public unsafe struct UnsafeAlignedBufferAllocation<TElement> : IDisposable where TElement : unmanaged {
   private static int DefaultAlignment = 32; // arbitrary for avx aligned moves

   private int count;
   private TElement* pElements;

   public UnsafeAlignedBufferAllocation(int count) : this(count, DefaultAlignment) { }

   public UnsafeAlignedBufferAllocation(int count, int alignment) {
      this.count = count;
      this.pElements = (TElement*)NativeMemory.AlignedAlloc((nuint)(sizeof(TElement) * count), 64);
   }

   public UnsafeAlignedBufferAllocation(int count, int alignment, TElement zeroValue) : this(count, alignment) {
      Fill(zeroValue);
   }

   public int Count => count;

   public void Fill(TElement value) => new Span<TElement>(pElements, count).Fill(value);

   public TElement* GetElementPtr(int idx) => &pElements[idx];

   public int GetElementIndex(TElement* ptr) => (int)((nuint)ptr - (nuint)pElements) / sizeof(TElement);

   public ref TElement this[int idx] => ref pElements[idx];

   public void Dispose() {
      if (pElements == null) return;
      NativeMemory.AlignedFree(pElements);
      pElements = null;
   }
}

public unsafe struct UnsafeBufferAllocation<TElement> : IDisposable where TElement : unmanaged {
   private int count;
   private TElement* pElements;

   public UnsafeBufferAllocation(int count) {
      this.count = count;
      this.pElements = (TElement*)NativeMemory.Alloc((nuint)(sizeof(TElement) * count), 64);
   }

   public UnsafeBufferAllocation(int count, TElement zeroValue) : this(count) {
      new Span<TElement>(pElements, count).Fill(zeroValue);
   }

   public int Count => count;

   public TElement* GetEndPtr() => &pElements[count];

   public TElement* GetElementPtr(int idx) => &pElements[idx];

   public int GetElementIndex(TElement* ptr) => (int)((nuint)ptr - (nuint)pElements) / sizeof(TElement);

   public ref TElement this[int idx] => ref pElements[idx];

   public void Dispose() {
      if (pElements == null) return;
      NativeMemory.Free(pElements);
      pElements = null;
   }
}

public unsafe struct UnsafePointerBufferAllocation<TElement> : IDisposable where TElement : unmanaged {
   private UnsafeBufferAllocation<IntPtr> inner;

   public UnsafePointerBufferAllocation(int count) {
      inner = new(count);
   }

   public UnsafePointerBufferAllocation(int count, TElement* zeroValue) {
      inner = new(count, (IntPtr)zeroValue);
   }

   public int Count => inner.Count;

   public TElement** GetElementPtr(int idx) => (TElement**)inner.GetElementPtr(idx);

   public int GetElementIndex(TElement** ptr) => inner.GetElementIndex((IntPtr*)ptr);

   public TElement* this[int idx]
   {
      get => (TElement*)inner[idx];
      set => inner[idx] = (IntPtr)value;
   }

   public void Dispose() => inner.Dispose();
}

public unsafe struct UnsafeDoublePointerBufferAllocation<TElement> : IDisposable where TElement : unmanaged {
   private UnsafeBufferAllocation<IntPtr> inner;

   public UnsafeDoublePointerBufferAllocation(int count) {
      inner = new(count);
   }

   public UnsafeDoublePointerBufferAllocation(int count, TElement** zeroValue) {
      inner = new(count, (IntPtr)zeroValue);
   }

   public int Count => inner.Count;

   public TElement*** GetElementPtr(int idx) => (TElement***)inner.GetElementPtr(idx);

   public int GetElementIndex(TElement*** ptr) => inner.GetElementIndex((IntPtr*)ptr);

   public TElement** this[int idx]
   {
      get => (TElement**)inner[idx];
      set => inner[idx] = (IntPtr)value;
   }

   public void Dispose() => inner.Dispose();
}