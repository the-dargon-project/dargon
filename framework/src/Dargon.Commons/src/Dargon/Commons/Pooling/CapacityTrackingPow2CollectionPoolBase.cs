using System;
using System.Collections.Generic;

namespace Dargon.Commons.Pooling {
   /// <summary>
   /// Use this to create object pools of collections whose capacities are not trivially queried.
   ///
   /// As an example, one can create a pool of byte*s of certain sizes, or GPU Buffers with sizes not tracked.
   /// </summary>
   public class CapacityTrackingPow2CollectionPoolBase<TElement> : Pow2CollectionPoolBase<TElement> {
      private readonly Dictionary<TElement, int> capacityMap;

      public CapacityTrackingPow2CollectionPoolBase(string name, Func<int, IObjectPool<TElement>> poolFactory) : this(name, poolFactory, new()) { }

      private CapacityTrackingPow2CollectionPoolBase(string name, Func<int, IObjectPool<TElement>> poolFactory, Dictionary<TElement, int> capacityMap) : base(name, CreateCapacityTrackingPoolFactory(poolFactory, capacityMap)) {
         this.capacityMap = capacityMap;
      }

      private static Func<int, IObjectPool<TElement>> CreateCapacityTrackingPoolFactory(Func<int, IObjectPool<TElement>> poolFactory, Dictionary<TElement, int> capacityMap)
         => capacity => new CapacityTrackingPoolWrapper(
            capacity,
            poolFactory(capacity),
            capacityMap);

      protected override int Capacity(TElement collection) {
         return capacityMap[collection];
      }

      private class CapacityTrackingPoolWrapper : IObjectPool<TElement> {
         private readonly int capacity;
         private readonly IObjectPool<TElement> inner;
         private readonly Dictionary<TElement, int> capacityMap;

         public CapacityTrackingPoolWrapper(int capacity, IObjectPool<TElement> inner, Dictionary<TElement, int> capacityMap) {
            this.capacity = capacity;
            this.inner = inner;
            this.capacityMap = capacityMap;
         }

         public string Name => inner.Name;

         public int Count => inner.Count;

         public TElement TakeObject() {
            var res = inner.TakeObject();
            capacityMap.Add(res, capacity);
            return res;
         }

         public void ReturnObject(TElement item) {
            capacityMap.Remove(item);
            inner.ReturnObject(item);
         }
      }
   }
}