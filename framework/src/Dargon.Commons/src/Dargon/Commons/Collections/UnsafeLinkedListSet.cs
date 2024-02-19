using Dargon.Commons.Templating;

namespace Dargon.Commons.Collections;

public unsafe class UnsafeLinkedListSet {
   private IntrusiveStore* pHead;
   private IntrusiveStore* pTailSentinel;

   /// <summary>
   /// Must be aligned to at least 2 bytes
   /// </summary>
   public struct IntrusiveStore {
      public const nuint RemovedBit = 1;

      public IntrusiveStore* pNext; // low bit stores whether node is removed
   }

   public UnsafeLinkedListSet() {
      pTailSentinel = (IntrusiveStore*)(nuint.MaxValue & ~IntrusiveStore.RemovedBit); // magic value
      pHead = pTailSentinel;
   }

   public IntrusiveStore* HeadPtr => pHead;

   public bool TryRemoveFirst(out IntrusiveStore* pItem) {
      while (true) {
         if (pHead == pTailSentinel) {
            pItem = null;
            return false;
         }

         var cur = pHead;
         var removed = ((nuint)cur->pNext & IntrusiveStore.RemovedBit) != 0;

         if (removed) {
            cur->pNext = (IntrusiveStore*)((nuint)cur->pNext & ~IntrusiveStore.RemovedBit); // unset removed bit
         }

         pHead = cur->pNext;
         cur->pNext = null;

         if (!removed) {
            pItem = cur;
            return true;
         }
      }
   }

   public bool Contains(IntrusiveStore* pItem) {
      return pItem->pNext != null &&
             ((nuint)pItem->pNext & IntrusiveStore.RemovedBit) != 0;
   }

   public bool TryAdd(IntrusiveStore* pItem) {
      if (pItem->pNext != null) {
         return false; // Already in list (next not null)
      }

      pItem->pNext = pHead;
      pHead = pItem;
      return true;
   }

   public bool TryRemove(IntrusiveStore* pItem) {
      if (pItem->pNext == null) {
         return false;
      }

      if (((nuint)pItem->pNext & IntrusiveStore.RemovedBit) != 0) {
         return false; // already removed
      }

      pItem->pNext = (IntrusiveStore*)((nuint)pItem->pNext | IntrusiveStore.RemovedBit);
      return true;
   }
}

/// <typeparam name="TItem">Item 1 is PNext</typeparam>
public unsafe class UnsafeLinkedListSet<TItem, TItemToIntrusiveStoreOffset>
   where TItem : unmanaged
   where TItemToIntrusiveStoreOffset : struct, ITemplateNint {
   private readonly UnsafeLinkedListSet inner = new();

   public TItem* HeadPtr => Z(inner.HeadPtr);

   public bool TryRemoveFirst(out TItem* pItem) {
      pItem = inner.TryRemoveFirst(out var temp) ? Z(temp) : null;
      return pItem != null;
   }

   public bool Contains(TItem* pItem) => inner.Contains(Z(pItem));

   public bool TryAdd(TItem* pItem) => inner.TryAdd(Z(pItem));

   public bool TryRemove(TItem* pItem) => inner.TryRemove(Z(pItem));

   private UnsafeLinkedListSet.IntrusiveStore* Z(TItem* p) => (UnsafeLinkedListSet.IntrusiveStore*)((nint)p + default(TItemToIntrusiveStoreOffset).Value);
   private TItem* Z(UnsafeLinkedListSet.IntrusiveStore* p) => (TItem*)((nint)p - default(TItemToIntrusiveStoreOffset).Value);
}