using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using static System.Text.Json.JsonElement;

namespace Dargon.Commons.Collections {
   public struct EnumeratorToEnumerableAdapter<TItem, TEnumerator> : IEnumerable<TItem> where TEnumerator : struct, IEnumerator<TItem> {
      private readonly TEnumerator enumerator;

      public EnumeratorToEnumerableAdapter(TEnumerator enumerator) {
         this.enumerator = enumerator;
      }

      public TEnumerator GetEnumerator() => enumerator;
      IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => GetEnumerator();
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
   }

   public static class StructLinq {
      public static StructLinqRangeGenerator Enumerate(int count) => new(0, count - 1, 1);
      public static StructLinqRangeGenerator Range(int initialValue, int finalValue, int increment) => new(initialValue, finalValue, increment);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static T AsTypeHint<T>(this T x) => default;
      public static T HintElementType<T>(this IEnumerable<T> x) => default;

      public static StructLinq2<T, TInnerEnumerator> For<TInnerEnumerator, T>(TInnerEnumerator x, T tHint) where TInnerEnumerator : struct, IEnumerator<T> => new(x);
      public static StructLinq2<T, TInnerEnumerator> SL<TInnerEnumerator, T>(this TInnerEnumerator x, T tHint) where TInnerEnumerator : struct, IEnumerator<T> => new(x);

      public static StructLinqWhere<T, TInnerEnumerator> Where<T, TInnerEnumerator>(this TInnerEnumerator inner, Func<T, bool> cond) where TInnerEnumerator : struct, IEnumerator<T>
         => StructLinq<T>.Where(inner, cond);

      public static StructLinqWhere<T, TInnerEnumerator> Where<T, TInnerEnumerator>(this TInnerEnumerator inner, T typeHint, Func<T, bool> cond) where TInnerEnumerator : struct, IEnumerator<T>
         => StructLinq<T>.Where(inner, cond);

      public static StructLinqWhere<T, TInnerEnumerator> Where<T, TInnerEnumerator, TDelegateStaticAssertMemo>(this TInnerEnumerator inner, Func<T, bool> cond, TDelegateStaticAssertMemo dummy = default)
         where TInnerEnumerator : struct, IEnumerator<T>
         where TDelegateStaticAssertMemo : struct
         => StructLinq<T>.Where(inner, cond, dummy);

      public static StructLinqWhere<T, TInnerEnumerator> Where<T, TInnerEnumerator, TDelegateStaticAssertMemo>(this TInnerEnumerator inner, T typeHint, Func<T, bool> cond, TDelegateStaticAssertMemo dummy = default)
         where TInnerEnumerator : struct, IEnumerator<T>
         where TDelegateStaticAssertMemo : struct
         => StructLinq<T>.Where(inner, cond, dummy);

      public static StructLinqMap<T, TInnerEnumerator, U> Map<T, TInnerEnumerator, U>(this TInnerEnumerator inner, Func<T, U> mapper) where TInnerEnumerator : struct, IEnumerator<T>
         => StructLinq<T>.Map(inner, mapper);

      public static StructLinqMap<T, TInnerEnumerator, U> Map<T, TInnerEnumerator, U>(this TInnerEnumerator inner, T typeHint, Func<T, U> mapper) where TInnerEnumerator : struct, IEnumerator<T>
         => StructLinq<T>.Map(inner, mapper);

      public static StructLinqMap<T, TInnerEnumerator, U> Map<T, TInnerEnumerator, TDelegateStaticAssertMemo, U>(this TInnerEnumerator inner, Func<T, U> mapper, TDelegateStaticAssertMemo dummy = default)
         where TInnerEnumerator : struct, IEnumerator<T>
         where TDelegateStaticAssertMemo : struct
         => StructLinq<T>.Map(inner, mapper, dummy);

      public static StructLinqMap<T, TInnerEnumerator, U> Map<T, TInnerEnumerator, TDelegateStaticAssertMemo, U>(this TInnerEnumerator inner, T typeHint, Func<T, U> mapper, TDelegateStaticAssertMemo dummy = default)
         where TInnerEnumerator : struct, IEnumerator<T>
         where TDelegateStaticAssertMemo : struct
         => StructLinq<T>.Map(inner, mapper, dummy);

      public static StructLinqEnumerate<T, TInnerEnumerator> Enumerate<T, TInnerEnumerator>(this TInnerEnumerator inner) where TInnerEnumerator : struct, IEnumerator<T>
         => StructLinq<T>.Enumerate(inner);

      public static StructLinqEnumerate<T, TInnerEnumerator> Enumerate<T, TInnerEnumerator>(this TInnerEnumerator inner, T typeHint) where TInnerEnumerator : struct, IEnumerator<T>
         => StructLinq<T>.Enumerate(inner);

      public static T[] FastToArray<T, TEnumerator>(this TEnumerator enumerator, int length, T typeHint) where TEnumerator : IEnumerator<T> {
         var res = new T[length];
         
         var nextIndex = 0;
         while (enumerator.MoveNext()) {
            res[nextIndex] = enumerator.Current;
            nextIndex++;
         }

         nextIndex.AssertEquals(res.Length);
         return res;
      }
   }

   public struct StructLinq2<T, TInnerEnumerator> where TInnerEnumerator : struct, IEnumerator<T> {
      public TInnerEnumerator inner;

      public StructLinq2(TInnerEnumerator inner) {
         this.inner = inner;
      }

      public StructLinqWhere<T, TInnerEnumerator> Where(Func<T, bool> cond)
         => StructLinq<T>.Where(inner, cond);

      public StructLinqWhere<T, TInnerEnumerator> Where<TDelegateStaticAssertMemo>(Func<T, bool> cond, TDelegateStaticAssertMemo dummy = default)
         where TDelegateStaticAssertMemo : struct
         => StructLinq<T>.Where(inner, cond, dummy);

      public StructLinqMap<T, TInnerEnumerator, U> Map<U>(Func<T, U> mapper)
         => StructLinq<T>.Map(inner, mapper);

      public StructLinqMap<T, TInnerEnumerator, U> Map<TDelegateStaticAssertMemo, U>(Func<T, U> mapper, TDelegateStaticAssertMemo dummy = default)
         where TDelegateStaticAssertMemo : struct
         => StructLinq<T>.Map(inner, mapper, dummy);

      public StructLinqEnumerate<T, TInnerEnumerator> Enumerate()
         => StructLinq<T>.Enumerate(inner);
   }

   public struct StructLinqOfT<T> {
      public StructLinqWhere<T, TInnerEnumerator> Where<TInnerEnumerator>(TInnerEnumerator inner, Func<T, bool> cond) where TInnerEnumerator : struct, IEnumerator<T>
         => StructLinq<T>.Where(inner, cond);

      public static StructLinqWhere<T, TInnerEnumerator> Where<TInnerEnumerator, TDelegateStaticAssertMemo>(TInnerEnumerator inner, Func<T, bool> cond, TDelegateStaticAssertMemo dummy = default)
         where TInnerEnumerator : struct, IEnumerator<T>
         where TDelegateStaticAssertMemo : struct
         => StructLinq<T>.Where(inner, cond, dummy);

      public static StructLinqMap<T, TInnerEnumerator, U> Map<TInnerEnumerator, U>(TInnerEnumerator inner, Func<T, U> mapper) where TInnerEnumerator : struct, IEnumerator<T>
         => StructLinq<T>.Map(inner, mapper);

      public static StructLinqMap<T, TInnerEnumerator, U> Map<TInnerEnumerator, TDelegateStaticAssertMemo, U>(TInnerEnumerator inner, Func<T, U> mapper, TDelegateStaticAssertMemo dummy = default)
         where TInnerEnumerator : struct, IEnumerator<T>
         where TDelegateStaticAssertMemo : struct
         => StructLinq<T>.Map(inner, mapper, dummy);

      public static StructLinqEnumerate<T, TInnerEnumerator> Enumerate<TInnerEnumerator>(TInnerEnumerator inner) where TInnerEnumerator : struct, IEnumerator<T>
         => StructLinq<T>.Enumerate(inner);
   }


   public static class StructLinq<T> {
      public static StructLinqWhere<T, TInnerEnumerator> Where<TInnerEnumerator>(TInnerEnumerator inner, Func<T, bool> cond) where TInnerEnumerator : struct, IEnumerator<T> {
         return new StructLinqWhere<T, TInnerEnumerator>(inner, cond);
      }

      public static StructLinqWhere<T, TInnerEnumerator> Where<TInnerEnumerator, TDelegateStaticAssertMemo>(TInnerEnumerator inner, Func<T, bool> cond, TDelegateStaticAssertMemo dummy = default)
         where TInnerEnumerator : struct, IEnumerator<T>
         where TDelegateStaticAssertMemo : struct {
         DelegateMethodIsStatic<TDelegateStaticAssertMemo>.VerifyOnce(cond);
         return new StructLinqWhere<T, TInnerEnumerator>(inner, cond);
      }

      public static StructLinqMap<T, TInnerEnumerator, U> Map<TInnerEnumerator, U>(TInnerEnumerator inner, Func<T, U> mapper) where TInnerEnumerator : struct, IEnumerator<T> {
         return new StructLinqMap<T, TInnerEnumerator, U>(inner, mapper);
      }

      public static StructLinqMap<T, TInnerEnumerator, U> Map<TInnerEnumerator, TDelegateStaticAssertMemo, U>(TInnerEnumerator inner, Func<T, U> mapper, TDelegateStaticAssertMemo dummy = default)
         where TInnerEnumerator : struct, IEnumerator<T>
         where TDelegateStaticAssertMemo : struct {
         DelegateMethodIsStatic<TDelegateStaticAssertMemo>.VerifyOnce(mapper);
         return new StructLinqMap<T, TInnerEnumerator, U>(inner, mapper);
      }

      public static StructLinqEnumerate<T, TInnerEnumerator> Enumerate<TInnerEnumerator>(TInnerEnumerator inner) where TInnerEnumerator : struct, IEnumerator<T> {
         return new StructLinqEnumerate<T, TInnerEnumerator>(inner);
      }

      private static class DelegateMethodIsStatic<TFuncStaticAssertMemo> where TFuncStaticAssertMemo : struct {
         private static bool verified = false;

         // when invoked, the static assert is executed
         public static void VerifyOnce(Delegate del) {
            if (verified) return;

            var methodInfo = del.GetMethodInfo();
            methodInfo.IsStatic.AssertIsTrue();

            verified = true;
         }
      }
   }

   public struct StructLinqWhere<T, TInnerEnumerator> : IEnumerator<T>, IEnumerable<T> where TInnerEnumerator : IEnumerator<T> {
      private TInnerEnumerator inner;
      private Func<T, bool> cond;

      public StructLinqWhere(TInnerEnumerator inner, Func<T, bool> cond) {
         this.inner = inner;
         this.cond = cond;
      }

      public bool MoveNext() {
         // iterate inner iterator until current passes cond.
         while (inner.MoveNext()) {
            if (cond(inner.Current)) {
               return true;
            }
         }

         // if we pass the end of inner, we can't move to next.
         return false;
      }

      public void Reset() => inner.Reset();
      public T Current => inner.Current;
      object IEnumerator.Current => Current;
      public void Dispose() => inner.Dispose();

      public IEnumerator<T> GetEnumerator() => this;
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
   }

   public struct StructLinqMap<T, TInnerEnumerator, U> : IEnumerator<U>, IEnumerable<U> where TInnerEnumerator : IEnumerator<T> {
      private TInnerEnumerator inner;
      private Func<T, U> mapper;

      public StructLinqMap(TInnerEnumerator inner, Func<T, U> mapper) {
         this.inner = inner;
         this.mapper = mapper;
      }

      public bool MoveNext() => inner.MoveNext();
      public void Reset() => inner.Reset();
      public U Current => mapper(inner.Current);
      object IEnumerator.Current => Current;
      public void Dispose() => inner.Dispose();

      public IEnumerator<U> GetEnumerator() => this;
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
   }

   public struct StructLinqEnumerate<T, TInnerEnumerator> : IEnumerator<(int, T)>, IEnumerable<(int, T)> where TInnerEnumerator : IEnumerator<T> {
      private TInnerEnumerator inner;
      private int currentIndex;

      public StructLinqEnumerate(TInnerEnumerator inner) {
         this.inner = inner;
         this.currentIndex = -1;
      }

      public bool MoveNext() {
         if (!inner.MoveNext()) {
            currentIndex = -1;
            return false;
         }

         currentIndex++;
         return true;
      }

      public void Reset() {
         inner.Reset();
         currentIndex = -1;
      }

      public (int, T) Current => (currentIndex, inner.Current);
      object IEnumerator.Current => Current;
      public void Dispose() => inner.Dispose();

      public IEnumerator<(int, T)> GetEnumerator() => this;
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
   }

   public struct StructLinqRangeGenerator : IEnumerator<int>, IEnumerable<int> {
      private readonly int initialValue;
      private readonly int finalValue;
      private readonly int increment;
      private int currentValue;

      public StructLinqRangeGenerator(int initial, int final, int increment) {
         this.initialValue = initial;
         this.finalValue = final;
         this.increment = increment;
         this.currentValue = initial;
      }

      public bool MoveNext() {
         if (currentValue == finalValue) return false;
         
         currentValue += increment;
         return true;
      }

      public void Reset() {
         this.currentValue = initialValue;
      }

      public int Current => currentValue;
      object IEnumerator.Current => Current;

      public void Dispose() { }

      public StructLinqRangeGenerator GetEnumerator() => this;
      IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
   }

   public static class StructLinqGroupAdjacentBy_GroupingEnumerator_Extensions {
      public static StructLinqGroupAdjacentBy_GroupingEnumerator<TGroupKey, TItem, TInnerEnumerator> GroupAdjacentBy<TGroupKey, TItem, TInnerEnumerator>(
         this scoped ref TInnerEnumerator innerEnumerator,
         Func<TItem, TGroupKey> itemToKeyProjector,
         TItem itemTypeHint = default)
         where TGroupKey : IEquatable<TGroupKey>
         where TInnerEnumerator : struct, IEnumerator<TItem> {
         return new(ref innerEnumerator, itemToKeyProjector);
      }


      private static void GroupAdjacentByDemo() {
         var eal = new ExposedArrayList<int>();
         eal.AddRange(Arrays.Create(1232, i => i));
         var ealEnumerator = eal.GetEnumerator();
         foreach (var x in ealEnumerator.GroupAdjacentBy(i => 1, eal.HintElementType())) {
            foreach (var y in x.Group) { }
         }
      }
   }

   public ref struct StructLinqGroupAdjacentBy_GroupingEnumerator<TGroupKey, TItem, TInnerEnumerator>
      where TGroupKey : IEquatable<TGroupKey>
      where TInnerEnumerator : struct, IEnumerator<TItem> {

      private ref TInnerEnumerator inner;
      private readonly Func<TItem, TGroupKey> itemToKeyFunc;
      private bool isCurrentAccessible = false;

      public StructLinqGroupAdjacentBy_GroupingEnumerator(scoped ref TInnerEnumerator inner, Func<TItem, TGroupKey> itemToKeyFunc) {
         this.inner = inner;
         this.itemToKeyFunc = itemToKeyFunc;
      }

      public bool MoveNext() {
         if (!inner.MoveNext()) {
            isCurrentAccessible = false;
            return false;
         }

         isCurrentAccessible = true;
         return true;
      }

      public Current_t Current {
         get {
            isCurrentAccessible.AssertIsTrue();
            isCurrentAccessible = false;

            return new() {
               Key = itemToKeyFunc(inner.Current),
               Group = new(ref inner, itemToKeyFunc),
            };
         }
      }

      public StructLinqGroupAdjacentBy_GroupingEnumerator<TGroupKey, TItem, TInnerEnumerator> GetEnumerator() => this;

      public ref struct Current_t {
         public TGroupKey Key;
         public StructLinqGroupAdjacentBy_GroupEnumerator<TGroupKey, TItem, TInnerEnumerator> Group;
      }
   }

   public ref struct StructLinqGroupAdjacentBy_GroupEnumerator<TGroupKey, TItem, TInnerEnumerator>
      where TGroupKey : IEquatable<TGroupKey>
      where TInnerEnumerator : IEnumerator<TItem> {

      private const int kModeNoopFirstMoveNext = 0;
      private const int kModeInitializedAndConsumingInner = 1;
      private const int kModeHalted = 2;
      
      private readonly ref TInnerEnumerator inner;
      private readonly Func<TItem, TGroupKey> itemToKeyFunc;
      private int mode;
      private TItem current;
      private TGroupKey groupKey;

      public StructLinqGroupAdjacentBy_GroupEnumerator(ref TInnerEnumerator inner, Func<TItem, TGroupKey> itemToKeyFunc) {
         this.inner = inner;
         this.itemToKeyFunc = itemToKeyFunc;

         this.mode = kModeNoopFirstMoveNext;
         this.current = inner.Current;
         this.groupKey = itemToKeyFunc(current);
      }

      public bool MoveNext() {
         if (this.mode == kModeNoopFirstMoveNext) {
            return true;
         } else if (this.mode == kModeHalted) {
            return false;
         }

         this.mode.AssertEquals(kModeInitializedAndConsumingInner);
         if (!inner.MoveNext()) {
            mode = kModeHalted;
            current = default;
            return false;
         }

         var innerCurrent = inner.Current;
         var innerCurrentKey = itemToKeyFunc(innerCurrent);
         if (!innerCurrentKey.Equals(groupKey)) {
            mode = kModeHalted;
            current = default;
            return false;
         }

         current = inner.Current;
         return true;
      }

      public void Reset() => throw new InvalidOperationException();

      public TItem Current => current;

      public void Dispose() {
         throw new NotImplementedException();
      }

      public StructLinqGroupAdjacentBy_GroupEnumerator<TGroupKey, TItem, TInnerEnumerator> GetEnumerator() => this;
   }

   public struct ArrayEnumerator2<T> : IEnumerator<T> {
      private readonly T[] arr;
      private int i;

      public ArrayEnumerator2(T[] arr) {
         this.arr = arr;
         this.i = -1;
      }

      public bool MoveNext() {
         if (i + 1 == arr.Length) {
            return false;
         }

         i++;
         return true;
      }

      public void Reset() => i = -1;

      public T Current => arr[i];
      object IEnumerator.Current => Current;

      public void Dispose() { }
   }

   public static class ArrayEnumeratorExtensions {
      public static StructLinq2<T, ArrayEnumerator2<T>> SL<T>(this ArrayEnumerator2<T> arr) => new(arr);
      public static ArrayEnumerator2<T> GetStructEnumerator<T>(this T[] arr) => new(arr);

   }

   public static class EnumeratorToEnumerableAdapter<TItem> {
      public static EnumeratorToEnumerableAdapter<TItem, TEnumerator> Create<TEnumerator>(TEnumerator enumerator) where TEnumerator : struct, IEnumerator<TItem> {
         return new EnumeratorToEnumerableAdapter<TItem, TEnumerator>(enumerator);
      }
   }
}