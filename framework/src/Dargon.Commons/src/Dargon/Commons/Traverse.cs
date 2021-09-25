using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Dargon.Commons.Collections;
using Dargon.Commons.Pooling;
using Dargon.Commons.Templating;

namespace Dargon.Commons {
   public static class Traverse {
      public struct TraversalEnumeratorBase<T, TCollection> : IEnumerator<T> 
         where TCollection : IReadOnlyCollection<T> {
         private TCollection store;
         private T initial;
         private Func<TCollection, T> popNext;
         private Action<TCollection, T> pushSuccs;
         private Action clearState;
         private IDisposable optDisposable;
         private T current;
         private (bool exists, T val) nextOpt;

         internal TraversalEnumeratorBase(TCollection store, T initial, Func<TCollection, T> popNext, Action<TCollection, T> pushSuccs, Action clearState, IDisposable optDisposable) {
            this.store = store;
            this.initial = initial;
            this.popNext = popNext;
            this.pushSuccs = pushSuccs;
            this.clearState = clearState;
            this.optDisposable = optDisposable;
            this.current = default;
            this.nextOpt = default;
            
            Reset();
         }

         public T Current => current;
         object IEnumerator.Current => current;

         public bool MoveNext() {
            if (!nextOpt.exists) {
               current = default;
               return false;
            }
            
            current = nextOpt.val;
            pushSuccs(store, current);
            nextOpt = store.Count > 0 ? (true, popNext(store)) : (false, default);
            return true;
         }

         public void Reset() {
            current = default;
            nextOpt = (true, initial);
            clearState();
         }

         public void Dispose() {
            optDisposable?.Dispose();
         }
      }

      public class TraversalEnumerator {
         public static TraversalEnumeratorBase<T, TCollection> Create<T, TCollection>(
            TCollection store,
            T initial, 
            Func<TCollection, T> popNext, 
            Action<TCollection, T> pushSuccs,
            Action clearState,
            IDisposable optDisposable
         )  where TCollection : IReadOnlyCollection<T> {
            return new TraversalEnumeratorBase<T, TCollection>(store, initial, popNext, pushSuccs, clearState, optDisposable);
         }
      }

      private static class DfsInternals<T, TBoolHandleDuplicatesAndLoops> {
         private static readonly TlsBackedObjectPool<State> tlsStates = new TlsBackedObjectPool<State>(State.Create);

         public static EnumeratorToEnumerableAdapter<T, TraversalEnumeratorBase<T, Stack<T>>> Dive(T initial, Func<T, T> succUser) {
            var state = tlsStates.TakeObject();
            state.pushSuccsUserOpt = null;
            state.succUserOpt = succUser;
            return EnumeratorToEnumerableAdapter<T>.Create(TraversalEnumerator.Create(
               state.store,
               initial,
               state.popNext,
               state.pushSuccs,
               state.clearStates,
               state));
         }

         public static EnumeratorToEnumerableAdapter<T, TraversalEnumeratorBase<T, Stack<T>>> Dfs(T initial, Action<Action<T>, T> pushSuccsUser) {
            var state = tlsStates.TakeObject();
            state.pushSuccsUserOpt = pushSuccsUser;
            state.succUserOpt = null;
            return EnumeratorToEnumerableAdapter<T>.Create(TraversalEnumerator.Create(
               state.store,
               initial,
               state.popNext,
               state.pushSuccs,
               state.clearStates,
               state));
         }

         public class State : IDisposable {
            public IObjectPool<State> pool;
            public Stack<T> store;
            public Stack<T> intermediateReverseStore;
            public HashSet<T> visited;
            public Func<Stack<T>, T> popNext;
            public Action<Stack<T>, T> pushSuccs;
            public Action clearStates;
            public Action<Action<T>, T> pushSuccsUserOpt;
            public Func<T, T> succUserOpt;

            public static State Create(IObjectPool<State> pool) {
               var s = new State();
               s.pool = pool;
               s.store = new Stack<T>();
               s.intermediateReverseStore = new Stack<T>();
               s.visited = TBool.IsTrue<TBoolHandleDuplicatesAndLoops>() ? new HashSet<T>() : null;
               s.popNext = store => store.Pop();

               Action<T> intermediateReverseStorePushCallback = x => {
                  if (TBool.IsFalse<TBoolHandleDuplicatesAndLoops>() || s.visited.Add(x)) {
                     s.intermediateReverseStore.Push(x);
                  }
               };
               s.pushSuccs = (_, el) => {
                  if (s.pushSuccsUserOpt != null) {
                     s.pushSuccsUserOpt(intermediateReverseStorePushCallback, el);
                     while (s.intermediateReverseStore.Count > 0) {
                        s.store.Push(s.intermediateReverseStore.Pop());
                     }
                  }
                  if (s.succUserOpt != null) {
                     var item = s.succUserOpt(el);
                     if (item != null) {
                        s.store.Push(item);
                     }
                  }
               };
               s.clearStates = () => {
                  s.store.Clear();
                  if (TBool.IsTrue<TBoolHandleDuplicatesAndLoops>()) {
                     s.visited.Clear();
                  }
               };
               s.pushSuccsUserOpt = null;
               return s;
            }

            public void Dispose() {
               pool.ReturnObject(this);
            }
         }
      }

      public static EnumeratorToEnumerableAdapter<T, TraversalEnumeratorBase<T, Stack<T>>> DfsWithDedupe<T>(
         this T root,
         Action<Action<T>, T> pushSuccsUser
      ) {
         return DfsInternals<T, TTrue>.Dfs(root, pushSuccsUser);
      }

      public static EnumeratorToEnumerableAdapter<T, TraversalEnumeratorBase<T, Stack<T>>> DfsWithoutDedupe<T>(
         this T root,
         Action<Action<T>, T> pushSuccsUser
      ) {
         return DfsInternals<T, TFalse>.Dfs(root, pushSuccsUser);
      }

      private static class BfsInternals<T, TBoolHandleDuplicatesAndLoops> {
         private static readonly TlsBackedObjectPool<State> tlsStates = new TlsBackedObjectPool<State>(State.Create);

         public static EnumeratorToEnumerableAdapter<T, TraversalEnumeratorBase<T, Queue<T>>> Bfs(T initial, Action<Action<T>, T> pushSuccsUser) {
            var state = tlsStates.TakeObject();
            state.pushSuccsUser = pushSuccsUser;
            return EnumeratorToEnumerableAdapter<T>.Create(TraversalEnumerator.Create(
               state.store,
               initial,
               state.popNext,
               state.pushSuccs,
               state.clear,
               state));
         }

         public class State : IDisposable {
            public IObjectPool<State> pool;
            public Queue<T> store;
            public HashSet<T> visited;
            public Func<Queue<T>, T> popNext;
            public Action<Queue<T>, T> pushSuccs;
            public Action clear;
            public Action<Action<T>, T> pushSuccsUser;

            public static State Create(IObjectPool<State> pool) {
               var s = new State();
               s.pool = pool;
               s.store = new Queue<T>();
               s.visited = TBool.IsTrue<TBoolHandleDuplicatesAndLoops>() ? new HashSet<T>() : null;
               s.popNext = store => store.Dequeue();
               Action<T> storeEnqueueConditionally = x => {
                  if (TBool.IsFalse<TBoolHandleDuplicatesAndLoops>() || s.visited.Add(x)) {
                     s.store.Enqueue(x);
                  }
               };
               s.pushSuccs = (_, el) => s.pushSuccsUser(storeEnqueueConditionally, el);
               s.clear = () => {
                  s.store.Clear();
                  if (TBool.IsTrue<TBoolHandleDuplicatesAndLoops>()) {
                     s.visited.Clear();
                  }
               };
               s.pushSuccsUser = null;
               return s;
            }

            public void Dispose() {
               pool.ReturnObject(this);
            }
         }
      }

      public static EnumeratorToEnumerableAdapter<T, TraversalEnumeratorBase<T, Queue<T>>> BfsWithDedupe<T>(this T root, Action<Action<T>, T> pushSuccsUser) {
         return BfsInternals<T, TTrue>.Bfs(root, pushSuccsUser);
      }

      public static EnumeratorToEnumerableAdapter<T, TraversalEnumeratorBase<T, Queue<T>>> BfsWithoutDedupe<T>(this T root, Action<Action<T>, T> pushSuccsUser) {
         return BfsInternals<T, TFalse>.Bfs(root, pushSuccsUser);
      }

      public static EnumeratorToEnumerableAdapter<T, TraversalEnumeratorBase<T, Stack<T>>> Dive<T>(this T root, Func<T, T> next) {
         return DfsInternals<T, TFalse>.Dive(root, next);
      }
   }
}
