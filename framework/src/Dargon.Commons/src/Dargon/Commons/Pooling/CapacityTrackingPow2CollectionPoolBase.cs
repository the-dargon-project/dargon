using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Dargon.Commons.Pooling {
   public interface ICapacityTracker<TCollectionLike> {
      void Initialize();
      void AssertInitialized();
      void Add(TCollectionLike collectionlike, int capacity);
      void Remove(TCollectionLike collectionlike);
      int Query(TCollectionLike collectionlike);
   }

   public struct ThreadSafeCapacityTracker<TCollectionLike> : ICapacityTracker<TCollectionLike> {
      private ConcurrentDictionary<TCollectionLike, int> store;

      public void Initialize() {
         store = new();
      }

      public void AssertInitialized() {
         store.AssertIsNotNull();
      }

      public void Add(TCollectionLike collectionlike, int capacity) {
         store.AddOrThrow(collectionlike, capacity);
      }

      public void Remove(TCollectionLike collectionlike) {
         store.RemoveOrThrow(collectionlike);
      }

      public int Query(TCollectionLike collectionlike) {
         return store[collectionlike];
      }
   }

   public struct ThreadUnsafeCapacityTracker<TCollectionLike> : ICapacityTracker<TCollectionLike> {
      private Dictionary<TCollectionLike, int> store;

      public void Initialize() {
         store = new();
      }

      public void AssertInitialized() {
         store.AssertIsNotNull();
      }

      public void Add(TCollectionLike collectionlike, int capacity) {
         store.Add(collectionlike, capacity);
      }

      public void Remove(TCollectionLike collectionlike) {
         store.Remove(collectionlike).AssertIsTrue();
      }

      public int Query(TCollectionLike collectionlike) {
         return store[collectionlike];
      }
   }

   public class ConcurrentDictionaryBackedCapacityTrackingPow2CollectionPoolBase<TElement> : CapacityTrackingPow2CollectionPoolBase<TElement, ThreadSafeCapacityTracker<TElement>> {
      public ConcurrentDictionaryBackedCapacityTrackingPow2CollectionPoolBase(string name, Func<int, IObjectPool<TElement>> poolFactory) : base(name, poolFactory) { }
   }

   public class ThreadUnsafeCapacityTrackingPow2CollectionPoolBase<TElement> : CapacityTrackingPow2CollectionPoolBase<TElement, ThreadUnsafeCapacityTracker<TElement>> {
      public ThreadUnsafeCapacityTrackingPow2CollectionPoolBase(string name, Func<int, IObjectPool<TElement>> poolFactory) : base(name, poolFactory) { }
   }

   /// <summary>
   /// Use this to create object pools of collections whose capacities are not trivially queried.
   ///
   /// As an example, one can create a pool of byte*s of certain sizes, or GPU Buffers with sizes not tracked.
   /// </summary>
   public class CapacityTrackingPow2CollectionPoolBase<TElement, TCapacityTracker> : Pow2CollectionPoolBase<TElement> where TCapacityTracker : struct, ICapacityTracker<TElement> {
      private class CapacityTrackerWrapper {
         public TCapacityTracker Inner;

         public CapacityTrackerWrapper() {
            Inner = new TCapacityTracker();
            Inner.Initialize();
            Inner.AssertInitialized();
         }
      }
      
      private readonly CapacityTrackerWrapper capacityTrackerWrapper;

      internal CapacityTrackingPow2CollectionPoolBase(string name, Func<int, IObjectPool<TElement>> poolFactory) : this(name, poolFactory, new()) { }

      private CapacityTrackingPow2CollectionPoolBase(string name, Func<int, IObjectPool<TElement>> poolFactory, CapacityTrackerWrapper capacityTrackerWrapper) : base(name, CreateCapacityTrackingPoolFactory(poolFactory, capacityTrackerWrapper)) {
         this.capacityTrackerWrapper = capacityTrackerWrapper;
      }

      private static Func<int, IObjectPool<TElement>> CreateCapacityTrackingPoolFactory(Func<int, IObjectPool<TElement>> poolFactory, CapacityTrackerWrapper capacityTrackerWrapper)
         => capacity => new CapacityTrackingPoolWrapper(
            capacity,
            poolFactory(capacity),
            capacityTrackerWrapper);

      protected override int Capacity(TElement collection) {
         return capacityTrackerWrapper.Inner.Query(collection);
      }

      private class CapacityTrackingPoolWrapper : IObjectPool<TElement> {
         private readonly int capacity;
         private readonly IObjectPool<TElement> inner;
         private readonly CapacityTrackerWrapper capacityTrackerWrapper;

         public CapacityTrackingPoolWrapper(int capacity, IObjectPool<TElement> inner, CapacityTrackerWrapper capacityTrackerWrapper) {
            this.capacity = capacity;
            this.inner = inner;
            this.capacityTrackerWrapper = capacityTrackerWrapper;
         }

         public string Name => inner.Name;

         public int Count => inner.Count;

         public TElement TakeObject() {
            var res = inner.TakeObject();
            capacityTrackerWrapper.Inner.Add(res, capacity);
            return res;
         }

         public void ReturnObject(TElement item) {
            capacityTrackerWrapper.Inner.Remove(item);
            inner.ReturnObject(item);
         }
      }
   }
}