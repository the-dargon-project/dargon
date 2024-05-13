using System.Numerics;
using Dargon.Commons.Collections.UnsafeCollections;
using Dargon.Commons.Utilities;

namespace Dargon.Commons.Collections {
   public unsafe class FixedSizeSeparateChainedUniquelyFastHashedDistinctDictionary<K, V>
      where K : unmanaged, IEqualityOperators<K, K, bool>
      where V : unmanaged {

      private readonly int capacity;
      private UnsafeBufferAllocation<Node> nodes;
      private Node* pFreeListHead;
      private Node* pFreeListEnd;
      private UnsafePointerBufferAllocation<Node> buckets;

      public struct Node {
         public Node* pNext;
         public K key;
         public V value;
      }

      public FixedSizeSeparateChainedUniquelyFastHashedDistinctDictionary(int capacity) {
         this.capacity = capacity;
         this.nodes = new(capacity); // not zeroed
         this.buckets = new(Primes.Ceil(capacity), null);

         pFreeListHead = nodes.GetElementPtr(0);
         for (var i = 0; i < capacity; i++) {
            nodes[i].pNext = nodes.GetElementPtr(i + 1);
         }
      }

      public void AddDistinct(K key, V value) {
         // remove from free list
         var pNode = pFreeListHead;
         pFreeListHead = pNode->pNext;

         // add node to bucket list
         var h = key.GetHashCode();
         var bucket = ComputeBucket(h);
         var ppBucketHead = buckets.GetElementPtr(bucket);
         pNode->pNext = *ppBucketHead;
         *ppBucketHead = pNode;

         // update node
         pNode->key = key;
         pNode->value = value;
      }

      public bool TryGetValue(K key, out V value) {
         var h = key.GetHashCode();
         var bucket = ComputeBucket(h);
         var pCur = buckets[bucket];
         
         while (pCur != null) {
            if (pCur->key == key) {
               value = pCur->value;
               return true;
            }

            pCur = pCur->pNext;
         }

         value = default;
         return false;
      }

      private int ComputeBucket(int hash) => (int)((uint)hash % buckets.Count);

      public void Dispose() {
         buckets.Dispose();
      }
   }
}
