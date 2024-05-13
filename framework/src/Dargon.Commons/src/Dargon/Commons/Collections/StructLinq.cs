using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Dargon.Commons.Utilities;

namespace Dargon.Commons.Collections;

public static class StructLinqExtensions {
   public static StructLinq2<T, ArrayEnumerator<T>> SL<T>(this T[] self) => new(new(self));
   public static StructLinq2<T, ArrayEnumerator<T>> SL<T>(this ArrayEnumerator<T> arr) => new(arr);
   public static StructLinq2<T, ArrayEnumerator2<T>> SL<T>(this ArrayEnumerator2<T> arr) => new(arr);
   public static StructLinq2<T, List<T>.Enumerator> SL<T>(this List<T> self) => new(self.GetEnumerator());
   public static StructLinq2<T, List<T>.Enumerator> SL<T>(this List<T>.Enumerator self) => new(self);
   public static StructLinq2<T, ExposedArrayList<T>.Enumerator> SL<T>(this ExposedArrayList<T> self) => new(self.GetEnumerator());
   public static StructLinq2<T, ExposedArrayList<T>.Enumerator> SL<T>(this ExposedArrayList<T>.Enumerator self) => new(self);
   public static StructLinq2<ExposedKeyValuePair<K, V>, ExposedArrayList<ExposedKeyValuePair<K, V>>.Enumerator> SL<K, V>(this ExposedListDictionary<K, V> self) => new(self.list.GetEnumerator());
   public static StructLinq2<KeyValuePair<K, V>, Dictionary<K, V>.Enumerator> SL<K, V>(this Dictionary<K, V> self) => new(self.GetEnumerator());

   public static (ArrayEnumerator<T>, T) __<T>(this T[] self) => (new(self), default);
   public static (List<T>.Enumerator, T) __<T>(this List<T> self) => (self.GetEnumerator(), default);
   public static (ExposedArrayList<T>.Enumerator, T) __<T>(this ExposedArrayList<T> self) => (self.GetEnumerator(), default);
   public static (ExposedArrayList<ExposedKeyValuePair<K, V>>.Enumerator, ExposedKeyValuePair<K, V>) __<K, V>(this ExposedListDictionary<K, V> self) => (self.GetEnumerator(), default);

   public static StructLinqDictionaryProjector<K, V> Projector<K, V>(this Dictionary<K, V> self) => new(self);
   public static (StructLinqDictionaryProjector<K, V>, V) _P<K, V>(this Dictionary<K, V> self) => (new(self), default);

   private static void CompilationTests() {
      var ints = new int[0];
      var dates = new DateTime[0];
      foreach (var x in ints.SL().Zip(dates.SL().Enumerate().Enumerate().Where(x => x.Item1 % 2 == 0).Enumerate().__)
                   .Zip(ints.SL().Enumerate().__)._) {
      }
   }
}

public static class StructLinq {
    public static StructLinq2<T, StructLinqRepeatGenerator<T>> Repeat<T>(T val) => new(new(val));
    public static StructLinq2<int, StructLinqRangeGenerator> Enumerate(int count) => new(new(0, count - 1, 1));
    public static StructLinq2<int, StructLinqRangeGenerator> Range(int initialValue, int finalValue, int increment) => new(new(initialValue, finalValue, increment));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T AsTypeHint<T>(this T x) => default;
    public static T HintElementType<T>(this IEnumerable<T> x) => default;

    public static StructLinq2<T, TInnerEnumerator> For<TInnerEnumerator, T>(TInnerEnumerator x, T tHint) where TInnerEnumerator : struct, IEnumerator<T> => new(x);
    public static StructLinq2<T, TInnerEnumerator> SL<TInnerEnumerator, T>(this TInnerEnumerator x, T tHint) where TInnerEnumerator : struct, IEnumerator<T> => new(x);

//    public static StructLinq2<T, StructLinqWhere<T, TInnerEnumerator>> Where<T, TInnerEnumerator>(this TInnerEnumerator inner, Func<T, bool> cond) where TInnerEnumerator : struct, IEnumerator<T>
//       => StructLinq<T>.Where(inner, cond).SL();
//
//    public static StructLinq2<T, StructLinqWhere<T, TInnerEnumerator>> Where<T, TInnerEnumerator>(this TInnerEnumerator inner, T typeHint, Func<T, bool> cond) where TInnerEnumerator : struct, IEnumerator<T>
//       => StructLinq<T>.Where(inner, cond). SL();
//
//    public static StructLinq2<T, StructLinqWhere<T, TInnerEnumerator>> Where<T, TInnerEnumerator, TDelegateStaticAssertMemo>(this TInnerEnumerator inner, Func<T, bool> cond, TDelegateStaticAssertMemo dummy = default)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where TDelegateStaticAssertMemo : struct
//       => StructLinq<T>.Where(inner, cond, dummy).SL();
//
//    public static StructLinq2<T, StructLinqWhere<T, TInnerEnumerator>> Where<T, TInnerEnumerator, TDelegateStaticAssertMemo>(this TInnerEnumerator inner, T typeHint, Func<T, bool> cond, TDelegateStaticAssertMemo dummy = default)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where TDelegateStaticAssertMemo : struct
//       => StructLinq<T>.Where(inner, cond, dummy).SL();
//
//    public static StructLinq2<U, StructLinqMap<T, TInnerEnumerator, U>> Map<T, TInnerEnumerator, U>(this TInnerEnumerator inner, Func<T, U> mapper) where TInnerEnumerator : struct, IEnumerator<T>
//       => StructLinq<T>.Map(inner, mapper).SL();
//
//    public static StructLinq2<U, StructLinqMap<T, TInnerEnumerator, U>> Map<T, TInnerEnumerator, U>(this TInnerEnumerator inner, T typeHint, Func<T, U> mapper) where TInnerEnumerator : struct, IEnumerator<T>
//       => StructLinq<T>.Map(inner, mapper).SL();
//
//    public static StructLinq2<U, StructLinqMap<T, TInnerEnumerator, U>> Map<T, TInnerEnumerator, TDelegateStaticAssertMemo, U>(this TInnerEnumerator inner, Func<T, U> mapper, TDelegateStaticAssertMemo dummy = default)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where TDelegateStaticAssertMemo : struct
//       => StructLinq<T>.Map(inner, mapper, dummy).SL();
//
//    public static StructLinq2<U, StructLinqMap<T, TInnerEnumerator, U>> Map<T, TInnerEnumerator, TDelegateStaticAssertMemo, U>(this TInnerEnumerator inner, T typeHint, Func<T, U> mapper, TDelegateStaticAssertMemo dummy = default)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where TDelegateStaticAssertMemo : struct
//       => StructLinq<T>.Map(inner, mapper, dummy).SL();
//
//    public static StructLinq2<(T, U), StructLinqZip2<T, TInnerEnumerator, U, UInnerEnumerator>> Zip<T, TInnerEnumerator, U, UInnerEnumerator>(TInnerEnumerator innerT, UInnerEnumerator innerU, T tTypeHint = default, U uTypeHint = default)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where UInnerEnumerator : struct, IEnumerator<U>
//       => StructLinq<T>.Zip(innerT, innerU, default(T), default(U)).SL();
//
//    public static StructLinq2<(T, U), StructLinqZip2<T, TInnerEnumerator, U, UInnerEnumerator>> Zip<T, TInnerEnumerator, U, UInnerEnumerator>((TInnerEnumerator, T) t, (UInnerEnumerator, U) u)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where UInnerEnumerator : struct, IEnumerator<U>
//       => StructLinq<T>.Zip(t.Item1, u.Item1, default(T), default(U)).SL();
//
//    public static StructLinq2<(T, U, V), StructLinqZip3<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator>> Zip<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator>(TInnerEnumerator tInner, UInnerEnumerator uInner, VInnerEnumerator vInner, T tTypeHint = default, U uTypeHint = default, V vTypeHint = default)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where UInnerEnumerator : struct, IEnumerator<U>
//       where VInnerEnumerator : struct, IEnumerator<V>
//       => StructLinq<T>.Zip(tInner, uInner, vInner, default(T), default(U), default(V)).SL();
//
//    public static StructLinq2<(T, U, V), StructLinqZip3<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator>> Zip<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator>((TInnerEnumerator, T) t, (UInnerEnumerator, U) u, (VInnerEnumerator, V) v)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where UInnerEnumerator : struct, IEnumerator<U>
//       where VInnerEnumerator : struct, IEnumerator<V>
//       => StructLinq<T>.Zip(t.Item1, u.Item1, v.Item1, default(T), default(U), default(V)).SL();
//
//    public static StructLinq2<(T, U, V, W), StructLinqZip4<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator>> Zip<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator>(TInnerEnumerator tInner, UInnerEnumerator uInner, VInnerEnumerator vInner, WInnerEnumerator wInner, T tTypeHint = default, U uTypeHint = default, V vTypeHint = default, W wTypeHint = default)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where UInnerEnumerator : struct, IEnumerator<U>
//       where VInnerEnumerator : struct, IEnumerator<V>
//       where WInnerEnumerator : struct, IEnumerator<W>
//       => StructLinq<T>.Zip(tInner, uInner, vInner, wInner, default(T), default(U), default(V), default(W)).SL();
//
//    public static StructLinq2<(T, U, V, W), StructLinqZip4<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator>> Zip<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator>((TInnerEnumerator, T) t, (UInnerEnumerator, U) u, (VInnerEnumerator, V) v, (WInnerEnumerator, W) w)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where UInnerEnumerator : struct, IEnumerator<U>
//       where VInnerEnumerator : struct, IEnumerator<V>
//       where WInnerEnumerator : struct, IEnumerator<W>
//       => StructLinq<T>.Zip(t.Item1, u.Item1, v.Item1, w.Item1, default(T), default(U), default(V), default(W)).SL();
//
//    public static StructLinq2<(T, U, V, W, X), StructLinqZip5<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator, X, XInnerEnumerator>> Zip<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator, X, XInnerEnumerator>(TInnerEnumerator tInner, UInnerEnumerator uInner, VInnerEnumerator vInner, WInnerEnumerator wInner, XInnerEnumerator xInner, T tTypeHint = default, U uTypeHint = default, V vTypeHint = default, W wTypeHint = default, X xTypeHint = default)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where UInnerEnumerator : struct, IEnumerator<U>
//       where VInnerEnumerator : struct, IEnumerator<V>
//       where WInnerEnumerator : struct, IEnumerator<W>
//       where XInnerEnumerator : struct, IEnumerator<X>
//       => StructLinq<T>.Zip(tInner, uInner, vInner, wInner, xInner, default(T), default(U), default(V), default(W), default(X)).SL();
//
//    public static StructLinq2<(T, U, V, W, X), StructLinqZip5<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator, X, XInnerEnumerator>> Zip<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator, X, XInnerEnumerator>((TInnerEnumerator, T) t, (UInnerEnumerator, U) u, (VInnerEnumerator, V) v, (WInnerEnumerator, W) w, (XInnerEnumerator, X) x)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where UInnerEnumerator : struct, IEnumerator<U>
//       where VInnerEnumerator : struct, IEnumerator<V>
//       where WInnerEnumerator : struct, IEnumerator<W>
//       where XInnerEnumerator : struct, IEnumerator<X>
//       => StructLinq<T>.Zip(t.Item1, u.Item1, v.Item1, w.Item1, x.Item1, default(T), default(U), default(V), default(W), default(X)).SL();
//
//    public static StructLinq2<(int, T), StructLinqEnumerate<T, TInnerEnumerator>> Enumerate<T, TInnerEnumerator>(this TInnerEnumerator inner) where TInnerEnumerator : struct, IEnumerator<T>
//       => StructLinq<T>.Enumerate(inner).SL();
//
//    public static StructLinq2<(int, T), StructLinqEnumerate<T, TInnerEnumerator>> Enumerate<T, TInnerEnumerator>(this TInnerEnumerator inner, T typeHint) where TInnerEnumerator : struct, IEnumerator<T>
//       => StructLinq<T>.Enumerate(inner).SL();
//
//    public static T[] FastToArray<T, TEnumerator>(this TEnumerator enumerator, int length) where TEnumerator : struct, IEnumerator<T>
//       => StructLinq<T>.FastToArray(enumerator, length);
}

/// <summary>
/// Does not implement IEnumerable as that leads to erroneously
/// calling LINQ functions which box & are expensive. This can still
/// be enumerated by foreach though!
/// </summary>
public struct StructLinq2<T, TInnerEnumerator> // : IEnumerable<T> - DO NOT IMPLEMENT
   where TInnerEnumerator : struct, IEnumerator<T> {
   public TInnerEnumerator inner;

   public StructLinq2(TInnerEnumerator inner) {
      this.inner = inner;
   }

   public StructLinq2<T, TInnerEnumerator> SL() => this;
   public TInnerEnumerator _ => inner;
   public (TInnerEnumerator, T) __ => (inner, default);
   public TInnerEnumerator GetEnumerator() => inner;
   // IEnumerator<T> IEnumerable<T>.GetEnumerator() => inner; - DO NOT IMPLEMENT
   // IEnumerator IEnumerable.GetEnumerator() => inner; - DO NOT IMPLEMENT

   public StructLinq2<T, StructLinqWhere<T, TInnerEnumerator>> Where(Func<T, bool> cond)
      => StructLinq<T>.Where(inner, cond).SL();

   public StructLinq2<T, StructLinqWhere<T, TInnerEnumerator>> Where<TDelegateStaticAssertMemo>(Func<T, bool> cond, TDelegateStaticAssertMemo dummy = default)
      where TDelegateStaticAssertMemo : struct
      => StructLinq<T>.Where(inner, cond, dummy).SL();

   public StructLinq2<U, StructLinqMap<T, TInnerEnumerator, U>> Map<U>(Func<T, U> mapper)
      => StructLinq<T>.Map(inner, mapper).SL();

   public StructLinq2<U, StructLinqMap<T, TInnerEnumerator, U>> Map<TDelegateStaticAssertMemo, U>(Func<T, U> mapper, TDelegateStaticAssertMemo dummy = default)
      where TDelegateStaticAssertMemo : struct
      => StructLinq<T>.Map(inner, mapper, dummy).SL();

   public StructLinq2<U, StructLinqMap2<T, TInnerEnumerator, U, VContext>> Map<VContext, U>(VContext context, Func<T, VContext, U> mapper)
      => StructLinq<T>.Map(inner, context, mapper).SL();

   public StructLinq2<U, StructLinqMap2<T, TInnerEnumerator, U, VContext>> Map<VContext, TDelegateStaticAssertMemo, U>(VContext context, Func<T, VContext, U> mapper, TDelegateStaticAssertMemo dummy = default)
      where TDelegateStaticAssertMemo : struct
      => StructLinq<T>.Map(inner, context, mapper, dummy).SL();

   public StructLinq2<U, StructLinqMap2<T, TInnerEnumerator, U, VContext>> Map<VContext, U>(Func<T, VContext, U> mapper, VContext context)
      => StructLinq<T>.Map(inner, context, mapper).SL();

   public StructLinq2<U, StructLinqMap2<T, TInnerEnumerator, U, VContext>> Map<VContext, TDelegateStaticAssertMemo, U>(Func<T, VContext, U> mapper, VContext context, TDelegateStaticAssertMemo dummy = default)
      where TDelegateStaticAssertMemo : struct
      => StructLinq<T>.Map(inner, context, mapper, dummy).SL();

   public StructLinq2<(T, U), StructLinqZip2<T, TInnerEnumerator, U, UInnerEnumerator>> Zip<U, UInnerEnumerator>(UInnerEnumerator e, U uTypeHint = default)
      where UInnerEnumerator : struct, IEnumerator<U>
      => StructLinq<T>.Zip(inner, e, default(T), default(U)).SL();

   public StructLinq2<(T, U), StructLinqZip2<T, TInnerEnumerator, U, UInnerEnumerator>> Zip<U, UInnerEnumerator>((UInnerEnumerator inner, U) u)
      where UInnerEnumerator : struct, IEnumerator<U>
      => StructLinq<T>.Zip(inner, u.inner, default(T), default(U)).SL();

   public StructLinq2<(T, U, V), StructLinqZip3<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator>> Zip<U, UInnerEnumerator, V, VInnerEnumerator>(UInnerEnumerator uInner, VInnerEnumerator vInner, U uTypeHint = default, V vTypeHint = default)
      where UInnerEnumerator : struct, IEnumerator<U>
      where VInnerEnumerator : struct, IEnumerator<V>
      => StructLinq<T>.Zip(inner, uInner, vInner, default(T), default(U), default(V)).SL();

   public StructLinq2<(T, U, V), StructLinqZip3<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator>> Zip<U, UInnerEnumerator, V, VInnerEnumerator>((UInnerEnumerator, U) u, (VInnerEnumerator, V) v)
      where UInnerEnumerator : struct, IEnumerator<U>
      where VInnerEnumerator : struct, IEnumerator<V>
      => StructLinq<T>.Zip(inner, u.Item1, v.Item1, default(T), default(U), default(V)).SL();

   public StructLinq2<(T, U, V, W), StructLinqZip4<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator>> Zip<U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator>(UInnerEnumerator uInner, VInnerEnumerator vInner, WInnerEnumerator wInner, U uTypeHint = default, V vTypeHint = default, W wTypeHint = default)
      where UInnerEnumerator : struct, IEnumerator<U>
      where VInnerEnumerator : struct, IEnumerator<V>
      where WInnerEnumerator : struct, IEnumerator<W>
      => StructLinq<T>.Zip(inner, uInner, vInner, wInner, default(T), default(U), default(V), default(W)).SL();

   public StructLinq2<(T, U, V, W), StructLinqZip4<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator>> Zip<U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator>((UInnerEnumerator, U) u, (VInnerEnumerator, V) v, (WInnerEnumerator, W) w)
      where UInnerEnumerator : struct, IEnumerator<U>
      where VInnerEnumerator : struct, IEnumerator<V>
      where WInnerEnumerator : struct, IEnumerator<W>
      => StructLinq<T>.Zip(inner, u.Item1, v.Item1, w.Item1, default(T), default(U), default(V), default(W)).SL();

   public StructLinq2<(T, U, V, W, X), StructLinqZip5<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator, X, XInnerEnumerator>> Zip<U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator, X, XInnerEnumerator>(UInnerEnumerator uInner, VInnerEnumerator vInner, WInnerEnumerator wInner, XInnerEnumerator xInner, U uTypeHint = default, V vTypeHint = default, W wTypeHint = default, X xTypeHint = default)
      where UInnerEnumerator : struct, IEnumerator<U>
      where VInnerEnumerator : struct, IEnumerator<V>
      where WInnerEnumerator : struct, IEnumerator<W>
      where XInnerEnumerator : struct, IEnumerator<X>
      => StructLinq<T>.Zip(inner, uInner, vInner, wInner, xInner, default(T), default(U), default(V), default(W), default(X)).SL();

   public StructLinq2<(T, U, V, W, X), StructLinqZip5<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator, X, XInnerEnumerator>> Zip<U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator, X, XInnerEnumerator>((UInnerEnumerator, U) u, (VInnerEnumerator, V) v, (WInnerEnumerator, W) w, (XInnerEnumerator, X) x)
      where UInnerEnumerator : struct, IEnumerator<U>
      where VInnerEnumerator : struct, IEnumerator<V>
      where WInnerEnumerator : struct, IEnumerator<W>
      where XInnerEnumerator : struct, IEnumerator<X>
      => StructLinq<T>.Zip(inner, u.Item1, v.Item1, w.Item1, x.Item1, default(T), default(U), default(V), default(W), default(X)).SL();

   public StructLinq2<(T left, U right), StructLinqLeftInnerJoin<T, TInnerEnumerator, U, TUProjector>> LeftInnerJoin<U, TUProjector>(TUProjector projector, U uTypeHint = default)
      where TUProjector : struct, IStructLinqProjector<T, U>
      => StructLinq<T>.LeftInnerJoin<TInnerEnumerator, U, TUProjector>(inner, projector).SL();

   public StructLinq2<(T left, U right), StructLinqLeftInnerJoin<T, TInnerEnumerator, U, TUProjector>> LeftInnerJoin<U, TUProjector>((TUProjector, U) projAndU)
      where TUProjector : struct, IStructLinqProjector<T, U>
      => StructLinq<T>.LeftInnerJoin(inner, projAndU).SL();

   public StructLinq2<(T left, MaybeValue<U> right), StructLinqLeftOuterJoin<T, TInnerEnumerator, U, TUProjector>> LeftOuterJoin<U, TUProjector>(TUProjector projector, U uTypeHint = default)
      where TUProjector : struct, IStructLinqProjector<T, U>
      => StructLinq<T>.LeftOuterJoin<TInnerEnumerator, U, TUProjector>(inner, projector).SL();

   public StructLinq2<(T left, MaybeValue<U> right), StructLinqLeftOuterJoin<T, TInnerEnumerator, U, TUProjector>> LeftOuterJoin<U, TUProjector>((TUProjector, U) projAndU)
      where TUProjector : struct, IStructLinqProjector<T, U>
      => StructLinq<T>.LeftOuterJoin(inner, projAndU).SL();

   public StructLinq2<(int i, T value), StructLinqEnumerate<T, TInnerEnumerator>> Enumerate()
      => StructLinq<T>.Enumerate(inner).SL();

   public T[] FastToArray(int length)
      => StructLinq<T>.FastToArray(inner, length);
}

// public struct StructLinqOfT<T> {
//    public StructLinqWhere<T, TInnerEnumerator> Where<TInnerEnumerator>(TInnerEnumerator inner, Func<T, bool> cond) where TInnerEnumerator : struct, IEnumerator<T>
//       => StructLinq<T>.Where(inner, cond);
//
//    public static StructLinq2<T, StructLinqWhere<T, TInnerEnumerator>> Where<TInnerEnumerator, TDelegateStaticAssertMemo>(TInnerEnumerator inner, Func<T, bool> cond, TDelegateStaticAssertMemo dummy = default)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where TDelegateStaticAssertMemo : struct
//       => StructLinq<T>.Where(inner, cond, dummy).SL();
//
//    public static StructLinqMap<T, TInnerEnumerator, U> Map<TInnerEnumerator, U>(TInnerEnumerator inner, Func<T, U> mapper) where TInnerEnumerator : struct, IEnumerator<T>
//       => StructLinq<T>.Map(inner, mapper);
//
//    public static StructLinq2<U, StructLinqMap<T, TInnerEnumerator, U>> Map<TInnerEnumerator, TDelegateStaticAssertMemo, U>(TInnerEnumerator inner, Func<T, U> mapper, TDelegateStaticAssertMemo dummy = default)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where TDelegateStaticAssertMemo : struct
//       => StructLinq<T>.Map(inner, mapper, dummy).SL();
//
//    public StructLinq2<(T, U), StructLinqZip2<T, TInnerEnumerator, U, UInnerEnumerator>> Zip<TInnerEnumerator, U, UInnerEnumerator>(TInnerEnumerator innerT, UInnerEnumerator innerU, U uTypeHint = default)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where UInnerEnumerator : struct, IEnumerator<U>
//       => StructLinq<T>.Zip(innerT, innerU, default(T), default(U)).SL();
//
//    public StructLinq2<(T, U), StructLinqZip2<T, TInnerEnumerator, U, UInnerEnumerator>> Zip<TInnerEnumerator, U, UInnerEnumerator>(TInnerEnumerator innerT, (UInnerEnumerator, U) u)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where UInnerEnumerator : struct, IEnumerator<U>
//       => StructLinq<T>.Zip(innerT, u.Item1, default(T), default(U)).SL();
//
//    public StructLinq2<(T, U, V), StructLinqZip3<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator>> Zip<TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator>(TInnerEnumerator tInner, UInnerEnumerator uInner, VInnerEnumerator vInner, U uTypeHint = default, V vTypeHint = default)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where UInnerEnumerator : struct, IEnumerator<U>
//       where VInnerEnumerator : struct, IEnumerator<V>
//       => StructLinq<T>.Zip(tInner, uInner, vInner, default(T), default(U), default(V)).SL();
//
//    public StructLinq2<(T, U, V), StructLinqZip3<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator>> Zip<TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator>(TInnerEnumerator tInner, (UInnerEnumerator, U) u, (VInnerEnumerator, V) v)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where UInnerEnumerator : struct, IEnumerator<U>
//       where VInnerEnumerator : struct, IEnumerator<V>
//       => StructLinq<T>.Zip(tInner, u.Item1, v.Item1, default(T), default(U), default(V)).SL();
//
//    public StructLinq2<(T, U, V, W), StructLinqZip4<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator>> Zip<TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator>(TInnerEnumerator tInner, UInnerEnumerator uInner, VInnerEnumerator vInner, WInnerEnumerator wInner, U uTypeHint = default, V vTypeHint = default, W wTypeHint = default)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where UInnerEnumerator : struct, IEnumerator<U>
//       where VInnerEnumerator : struct, IEnumerator<V>
//       where WInnerEnumerator : struct, IEnumerator<W>
//       => StructLinq<T>.Zip(tInner, uInner, vInner, wInner, default(T), default(U), default(V), default(W)).SL();
//
//    public StructLinq2<(T, U, V, W), StructLinqZip4<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator>> Zip<TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator>(TInnerEnumerator tInner, (UInnerEnumerator, U) u, (VInnerEnumerator, V) v, (WInnerEnumerator, W) w)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where UInnerEnumerator : struct, IEnumerator<U>
//       where VInnerEnumerator : struct, IEnumerator<V>
//       where WInnerEnumerator : struct, IEnumerator<W>
//       => StructLinq<T>.Zip(tInner, u.Item1, v.Item1, w.Item1, default(T), default(U), default(V), default(W)).SL();
//
//    public StructLinq2<(T, U, V, W, X), StructLinqZip5<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator, X, XInnerEnumerator>> Zip<TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator, X, XInnerEnumerator>(TInnerEnumerator tInner, UInnerEnumerator uInner, VInnerEnumerator vInner, WInnerEnumerator wInner, XInnerEnumerator xInner, U uTypeHint = default, V vTypeHint = default, W wTypeHint = default, X xTypeHint = default)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where UInnerEnumerator : struct, IEnumerator<U>
//       where VInnerEnumerator : struct, IEnumerator<V>
//       where WInnerEnumerator : struct, IEnumerator<W>
//       where XInnerEnumerator : struct, IEnumerator<X>
//       => StructLinq<T>.Zip(tInner, uInner, vInner, wInner, xInner, default(T), default(U), default(V), default(W), default(X)).SL();
//
//    public StructLinq2<(T, U, V, W, X), StructLinqZip5<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator, X, XInnerEnumerator>> Zip<TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator, X, XInnerEnumerator>(TInnerEnumerator tInner, (UInnerEnumerator, U) u, (VInnerEnumerator, V) v, (WInnerEnumerator, W) w, (XInnerEnumerator, X) x)
//       where TInnerEnumerator : struct, IEnumerator<T>
//       where UInnerEnumerator : struct, IEnumerator<U>
//       where VInnerEnumerator : struct, IEnumerator<V>
//       where WInnerEnumerator : struct, IEnumerator<W>
//       where XInnerEnumerator : struct, IEnumerator<X>
//       => StructLinq<T>.Zip(tInner, u.Item1, v.Item1, w.Item1, x.Item1, default(T), default(U), default(V), default(W), default(X)).SL();
//
//    public static StructLinq2<(int, T), StructLinqEnumerate<T, TInnerEnumerator>> Enumerate<TInnerEnumerator>(TInnerEnumerator inner) where TInnerEnumerator : struct, IEnumerator<T>
//       => StructLinq<T>.Enumerate(inner).SL();
// }


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

   public static StructLinqMap2<T, TInnerEnumerator, U, VContext> Map<TInnerEnumerator, U, VContext>(TInnerEnumerator inner, VContext context, Func<T, VContext, U> mapper) where TInnerEnumerator : struct, IEnumerator<T> {
      return new StructLinqMap2<T, TInnerEnumerator, U, VContext>(inner, context, mapper);
   }

   public static StructLinqMap2<T, TInnerEnumerator, U, VContext> Map<TInnerEnumerator, TDelegateStaticAssertMemo, U, VContext>(TInnerEnumerator inner, VContext context, Func<T, VContext, U> mapper, TDelegateStaticAssertMemo dummy = default)
      where TInnerEnumerator : struct, IEnumerator<T>
      where TDelegateStaticAssertMemo : struct {
      DelegateMethodIsStatic<TDelegateStaticAssertMemo>.VerifyOnce(mapper);
      return new StructLinqMap2<T, TInnerEnumerator, U, VContext>(inner, context, mapper);
   }

   public static StructLinqZip2<T, TInnerEnumerator, U, UInnerEnumerator> Zip<T, TInnerEnumerator, U, UInnerEnumerator>(TInnerEnumerator inner, UInnerEnumerator uInnerEnumerator, T tTypeHint = default, U uTypeHint = default)
      where TInnerEnumerator : struct, IEnumerator<T>
      where UInnerEnumerator : struct, IEnumerator<U> {
      return new StructLinqZip2<T, TInnerEnumerator, U, UInnerEnumerator>(inner, uInnerEnumerator);
   }

   public static StructLinqZip3<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator> Zip<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator>(TInnerEnumerator inner, UInnerEnumerator uInnerEnumerator, VInnerEnumerator vInnerEnumerator, T tTypeHint = default, U uTypeHint = default, V vTypeHint = default)
      where TInnerEnumerator : struct, IEnumerator<T>
      where UInnerEnumerator : struct, IEnumerator<U>
      where VInnerEnumerator : struct, IEnumerator<V> {
      return new StructLinqZip3<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator>(inner, uInnerEnumerator, vInnerEnumerator);
   }

   public static StructLinqZip4<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator> Zip<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator>(TInnerEnumerator inner, UInnerEnumerator uInnerEnumerator, VInnerEnumerator vInnerEnumerator, WInnerEnumerator wInnerEnumerator, T tTypeHint = default, U uTypeHint = default, V vTypeHint = default, W wTypeHint = default)
      where TInnerEnumerator : struct, IEnumerator<T>
      where UInnerEnumerator : struct, IEnumerator<U>
      where VInnerEnumerator : struct, IEnumerator<V>
      where WInnerEnumerator : struct, IEnumerator<W> {
      return new StructLinqZip4<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator>(inner, uInnerEnumerator, vInnerEnumerator, wInnerEnumerator);
   }

   public static StructLinqZip5<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator, X, XInnerEnumerator> Zip<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator, X, XInnerEnumerator>(TInnerEnumerator inner, UInnerEnumerator uInnerEnumerator, VInnerEnumerator vInnerEnumerator, WInnerEnumerator wInnerEnumerator, XInnerEnumerator xInnerEnumerator, T tTypeHint = default, U uTypeHint = default, V vTypeHint = default, W wTypeHint = default, X xTypeHint = default)
      where TInnerEnumerator : struct, IEnumerator<T>
      where UInnerEnumerator : struct, IEnumerator<U>
      where VInnerEnumerator : struct, IEnumerator<V>
      where WInnerEnumerator : struct, IEnumerator<W>
      where XInnerEnumerator : struct, IEnumerator<X> {
      return new StructLinqZip5<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator, X, XInnerEnumerator>(inner, uInnerEnumerator, vInnerEnumerator, wInnerEnumerator, xInnerEnumerator);
   }

   public static StructLinqEnumerate<T, TInnerEnumerator> Enumerate<TInnerEnumerator>(TInnerEnumerator inner) where TInnerEnumerator : struct, IEnumerator<T> {
      return new StructLinqEnumerate<T, TInnerEnumerator>(inner);
   }

   public static StructLinqLeftInnerJoin<T, TInnerEnumerator, U, TUProjector> LeftInnerJoin<TInnerEnumerator, U, TUProjector>(TInnerEnumerator inner, TUProjector projector, U uTypeHint = default)
      where TInnerEnumerator : struct, IEnumerator<T>
      where TUProjector : struct, IStructLinqProjector<T, U> {
      return new(inner, projector);
   }

   public static StructLinqLeftInnerJoin<T, TInnerEnumerator, U, TUProjector> LeftInnerJoin<TInnerEnumerator, U, TUProjector>(TInnerEnumerator inner, (TUProjector, U) projAndU)
      where TInnerEnumerator : struct, IEnumerator<T>
      where TUProjector : struct, IStructLinqProjector<T, U> {
      return new(inner, projAndU.Item1);
   }

   public static StructLinqLeftOuterJoin<T, TInnerEnumerator, U, TUProjector> LeftOuterJoin<TInnerEnumerator, U, TUProjector>(TInnerEnumerator inner, TUProjector projector, U uTypeHint = default)
      where TInnerEnumerator : struct, IEnumerator<T>
      where TUProjector : struct, IStructLinqProjector<T, U> {
      return new(inner, projector);
   }

   public static StructLinqLeftOuterJoin<T, TInnerEnumerator, U, TUProjector> LeftOuterJoin<TInnerEnumerator, U, TUProjector>(TInnerEnumerator inner, (TUProjector, U) projAndU)
      where TInnerEnumerator : struct, IEnumerator<T>
      where TUProjector : struct, IStructLinqProjector<T, U> {
      return new(inner, projAndU.Item1);
   }

   public static T[] FastToArray<TInnerEnumerator>(TInnerEnumerator inner, int length) where TInnerEnumerator : struct, IEnumerator<T> {
      var res = new T[length];

      var nextIndex = 0;
      while (inner.MoveNext()) {
         res[nextIndex] = inner.Current;
         nextIndex++;
      }

      nextIndex.AssertEquals(res.Length);
      return res;
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

   public StructLinq2<T, StructLinqWhere<T, TInnerEnumerator>> SL() => new(this);
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

   public StructLinq2<U, StructLinqMap<T, TInnerEnumerator, U>> SL() => new(this);
}

public struct StructLinqMap2<T, TInnerEnumerator, U, VContext> : IEnumerator<U>, IEnumerable<U> where TInnerEnumerator : IEnumerator<T> {
   private TInnerEnumerator inner;
   private VContext context;
   private Func<T, VContext, U> mapper;

   public StructLinqMap2(TInnerEnumerator inner, VContext context, Func<T, VContext, U> mapper) {
      this.inner = inner;
      this.context = context;
      this.mapper = mapper;
   }

   public bool MoveNext() => inner.MoveNext();
   public void Reset() => inner.Reset();
   public U Current => mapper(inner.Current, context);
   object IEnumerator.Current => Current;
   public void Dispose() => inner.Dispose();

   public IEnumerator<U> GetEnumerator() => this;
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

   public StructLinq2<U, StructLinqMap2<T, TInnerEnumerator, U, VContext>> SL() => new(this);
}

public struct StructLinqZip2<T, TInnerEnumerator, U, UInnerEnumerator> : IEnumerator<(T, U)>, IEnumerable<(T, U)>
   where TInnerEnumerator : struct, IEnumerator<T>
   where UInnerEnumerator : struct, IEnumerator<U> {

   private TInnerEnumerator innerT;
   private UInnerEnumerator innerU;

   public StructLinqZip2(TInnerEnumerator innerT, UInnerEnumerator innerU) {
      this.innerT = innerT;
      this.innerU = innerU;
   }

   public bool MoveNext() => innerT.MoveNext() && innerU.MoveNext();

   public void Reset() {
      innerT.Reset();
      innerU.Reset();
   }

   public (T, U) Current => (innerT.Current, innerU.Current);
   object IEnumerator.Current => Current;

   public void Dispose() {
      innerT.Dispose();
      innerU.Dispose();
   }

   public IEnumerator<(T, U)> GetEnumerator() => this;
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

   public StructLinq2<(T, U), StructLinqZip2<T, TInnerEnumerator, U, UInnerEnumerator>> SL() => new(this);
}

public struct StructLinqZip3<
   T, TInnerEnumerator,
   U, UInnerEnumerator,
   V, VInnerEnumerator
> : IEnumerator<(T, U, V)>, IEnumerable<(T, U, V)>
   where TInnerEnumerator : struct, IEnumerator<T>
   where UInnerEnumerator : struct, IEnumerator<U>
   where VInnerEnumerator : struct, IEnumerator<V> {

   private TInnerEnumerator innerT;
   private UInnerEnumerator innerU;
   private VInnerEnumerator innerV;

   public StructLinqZip3(TInnerEnumerator innerT, UInnerEnumerator innerU, VInnerEnumerator innerV) {
      this.innerT = innerT;
      this.innerU = innerU;
      this.innerV = innerV;
   }

   public bool MoveNext() => innerT.MoveNext() && innerU.MoveNext() && innerV.MoveNext();

   public void Reset() {
      innerT.Reset();
      innerU.Reset();
      innerV.Reset();
   }

   public (T, U, V) Current => (innerT.Current, innerU.Current, innerV.Current);
   object IEnumerator.Current => Current;

   public void Dispose() {
      innerT.Dispose();
      innerU.Dispose();
      innerV.Dispose();
   }

   public IEnumerator<(T, U, V)> GetEnumerator() => this;
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

   public StructLinq2<(T, U, V), StructLinqZip3<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator>> SL() => new(this);
}

public struct StructLinqZip4<
   T, TInnerEnumerator,
   U, UInnerEnumerator,
   V, VInnerEnumerator,
   W, WInnerEnumerator
> : IEnumerator<(T, U, V, W)>, IEnumerable<(T, U, V, W)>
   where TInnerEnumerator : struct, IEnumerator<T>
   where UInnerEnumerator : struct, IEnumerator<U>
   where VInnerEnumerator : struct, IEnumerator<V>
   where WInnerEnumerator : struct, IEnumerator<W> {

   private TInnerEnumerator innerT;
   private UInnerEnumerator innerU;
   private VInnerEnumerator innerV;
   private WInnerEnumerator innerW;

   public StructLinqZip4(TInnerEnumerator innerT, UInnerEnumerator innerU, VInnerEnumerator innerV, WInnerEnumerator innerW) {
      this.innerT = innerT;
      this.innerU = innerU;
      this.innerV = innerV;
      this.innerW = innerW;
   }

   public bool MoveNext() => innerT.MoveNext() && innerU.MoveNext() && innerV.MoveNext() && innerW.MoveNext();

   public void Reset() {
      innerT.Reset();
      innerU.Reset();
      innerV.Reset();
      innerW.Reset();
   }

   public (T, U, V, W) Current => (innerT.Current, innerU.Current, innerV.Current, innerW.Current);
   object IEnumerator.Current => Current;

   public void Dispose() {
      innerT.Dispose();
      innerU.Dispose();
      innerV.Dispose();
      innerW.Dispose();
   }

   public IEnumerator<(T, U, V, W)> GetEnumerator() => this;
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

   public StructLinq2<(T, U, V, W), StructLinqZip4<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator>> SL() => new(this);
}

public struct StructLinqZip5<
   T, TInnerEnumerator,
   U, UInnerEnumerator,
   V, VInnerEnumerator,
   W, WInnerEnumerator,
   X, XInnerEnumerator
> : IEnumerator<(T, U, V, W, X)>, IEnumerable<(T, U, V, W, X)>
   where TInnerEnumerator : struct, IEnumerator<T>
   where UInnerEnumerator : struct, IEnumerator<U>
   where VInnerEnumerator : struct, IEnumerator<V>
   where WInnerEnumerator : struct, IEnumerator<W>
   where XInnerEnumerator : struct, IEnumerator<X> {

   private TInnerEnumerator innerT;
   private UInnerEnumerator innerU;
   private VInnerEnumerator innerV;
   private WInnerEnumerator innerW;
   private XInnerEnumerator innerX;

   public StructLinqZip5(TInnerEnumerator innerT, UInnerEnumerator innerU, VInnerEnumerator innerV, WInnerEnumerator innerW, XInnerEnumerator innerX) {
      this.innerT = innerT;
      this.innerU = innerU;
      this.innerV = innerV;
      this.innerW = innerW;
      this.innerX = innerX;
   }

   public bool MoveNext() => innerT.MoveNext() && innerU.MoveNext() && innerV.MoveNext() && innerW.MoveNext() && innerX.MoveNext();

   public void Reset() {
      innerT.Reset();
      innerU.Reset();
      innerV.Reset();
      innerW.Reset();
      innerX.Reset();
   }

   public (T, U, V, W, X) Current => (innerT.Current, innerU.Current, innerV.Current, innerW.Current, innerX.Current);
   object IEnumerator.Current => Current;

   public void Dispose() {
      innerT.Dispose();
      innerU.Dispose();
      innerV.Dispose();
      innerW.Dispose();
      innerX.Dispose();
   }

   public IEnumerator<(T, U, V, W, X)> GetEnumerator() => this;
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

   public StructLinq2<(T, U, V, W, X), StructLinqZip5<T, TInnerEnumerator, U, UInnerEnumerator, V, VInnerEnumerator, W, WInnerEnumerator, X, XInnerEnumerator>> SL() => new(this);
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

   public StructLinq2<(int, T), StructLinqEnumerate<T, TInnerEnumerator>> SL() => new(this);
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
      this.currentValue = initial - increment;
   }

   public bool MoveNext() {
      if (currentValue == finalValue) return false;

      currentValue += increment;
      return true;
   }

   public void Reset() {
      this.currentValue = initialValue - increment;
   }

   public int Current => currentValue;
   object IEnumerator.Current => Current;

   public void Dispose() { }

   public StructLinqRangeGenerator GetEnumerator() => this;
   IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

   public StructLinq2<int, StructLinqRangeGenerator> SL() => new(this);
}

public struct StructLinqRepeatGenerator<T> : IEnumerator<T>, IEnumerable<T> {
   private T value;

   public StructLinqRepeatGenerator(T value) => this.value = value;

   public bool MoveNext() => true;
   public void Reset() { }

   public T Current => value;
   object IEnumerator.Current => Current;

   public void Dispose() { }

   public StructLinqRepeatGenerator<T> GetEnumerator() => this;
   IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

   public StructLinq2<T, StructLinqRepeatGenerator<T>> SL() => new(this);
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

public interface IStructLinqProjector<T, U> {
   bool TryProject(T t, out U u);
}

public struct StructLinqDictionaryProjector<K, V> : IStructLinqProjector<K, V> {
   private readonly Dictionary<K, V> inner;
   public StructLinqDictionaryProjector(Dictionary<K, V> inner) => this.inner = inner;
   public bool TryProject(K k, out V v) => inner.TryGetValue(k, out v);
}

public struct StructLinqLeftInnerJoin<T, TInnerEnumerator, U, TUProjector> : IEnumerator<(T, U)>, IEnumerable<(T, U)>
   where TInnerEnumerator : struct, IEnumerator<T>
   where TUProjector : struct, IStructLinqProjector<T, U> {
   private TInnerEnumerator inner;
   private TUProjector projector;
   private U lastProjection;

   public StructLinqLeftInnerJoin(TInnerEnumerator inner, TUProjector projector) {
      this.inner = inner;
      this.projector = projector;
   }

   public bool MoveNext() {
      while (inner.MoveNext()) {
         if (projector.TryProject(inner.Current, out lastProjection)) {
            return true;
         }
      }

      return false;
   }

   public void Reset() => inner.Reset();
   public (T, U) Current => (inner.Current, lastProjection);
   object IEnumerator.Current => Current;
   public void Dispose() => inner.Dispose();

   public IEnumerator<(T, U)> GetEnumerator() => this;
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

   public StructLinq2<(T, U), StructLinqLeftInnerJoin<T, TInnerEnumerator, U, TUProjector>> SL() => new(this);
}

public struct StructLinqLeftOuterJoin<T, TInnerEnumerator, U, TUProjector> : IEnumerator<(T, MaybeValue<U>)>, IEnumerable<(T, MaybeValue<U>)>
   where TInnerEnumerator : struct, IEnumerator<T>
   where TUProjector : struct, IStructLinqProjector<T, U> {
   private TInnerEnumerator inner;
   private TUProjector projector;
   private MaybeValue<U> lastProjection;

   public StructLinqLeftOuterJoin(TInnerEnumerator inner, TUProjector projector) {
      this.inner = inner;
      this.projector = projector;
   }

   public bool MoveNext() {
      while (inner.MoveNext()) {
         lastProjection.HasValue = projector.TryProject(inner.Current, out lastProjection.Value);
         return true;
      }

      return false;
   }

   public void Reset() => inner.Reset();
   public (T, MaybeValue<U>) Current => (inner.Current, lastProjection);
   object IEnumerator.Current => Current;
   public void Dispose() => inner.Dispose();

   public IEnumerator<(T, MaybeValue<U>)> GetEnumerator() => this;
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

   public StructLinq2<(T, MaybeValue<U>), StructLinqLeftOuterJoin<T, TInnerEnumerator, U, TUProjector>> SL() => new(this);
}