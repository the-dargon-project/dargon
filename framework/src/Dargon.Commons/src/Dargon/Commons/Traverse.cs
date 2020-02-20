using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dargon.Commons.Collections;
using Dargon.Commons.Pooling;

namespace Dargon.Commons {
   public static class Traverse {
      public struct TraversalEnumeratorBase<T, TCollection> : IEnumerator<T> 
         where TCollection : IReadOnlyCollection<T> {
         private TCollection store;
         private T initial;
         private Func<TCollection, T> popNext;
         private Action<TCollection, T> pushSuccs;
         private Action<TCollection> clear;
         private IDisposable optDisposable;
         private T current;
         private (bool exists, T val) nextOpt;

         internal TraversalEnumeratorBase(TCollection store, T initial, Func<TCollection, T> popNext, Action<TCollection, T> pushSuccs, Action<TCollection> clear, IDisposable optDisposable) {
            this.store = store;
            this.initial = initial;
            this.popNext = popNext;
            this.pushSuccs = pushSuccs;
            this.clear = clear;
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
            clear(store);
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
            Action<TCollection> clear,
            IDisposable optDisposable
         )  where TCollection : IReadOnlyCollection<T> {
            return new TraversalEnumeratorBase<T, TCollection>(store, initial, popNext, pushSuccs, clear, optDisposable);
         }
      }

      private static class DfsInternals<T> {
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
               state.clear,
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
               state.clear,
               state));
         }

         public class State : IDisposable {
            public IObjectPool<State> pool;
            public Stack<T> store;
            public Func<Stack<T>, T> popNext;
            public Action<Stack<T>, T> pushSuccs;
            public Action<Stack<T>> clear;
            public Action<Action<T>, T> pushSuccsUserOpt;
            public Func<T, T> succUserOpt;

            public static State Create(IObjectPool<State> pool) {
               var s = new State();
               s.pool = pool;
               s.store = new Stack<T>();
               s.popNext = store => store.Pop();

               Action<T> storePush = s.store.Push;
               s.pushSuccs = (_, el) => {
                  if (s.pushSuccsUserOpt != null) {
                     s.pushSuccsUserOpt(storePush, el);
                  }
                  if (s.succUserOpt != null) {
                     s.store.Push(s.succUserOpt(el));
                  }
               };
               s.clear = store => store.Clear();
               s.pushSuccsUserOpt = null;
               return s;
            }

            public void Dispose() {
               pool.ReturnObject(this);
            }
         }
      }

      public static EnumeratorToEnumerableAdapter<T, TraversalEnumeratorBase<T, Stack<T>>> Dfs<T>(
         this T root, 
         Action<Action<T>, T> pushSuccsUser
      ) {
         return DfsInternals<T>.Dfs(root, pushSuccsUser);
      }

      private static class BfsInternals<T> {
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
            public Func<Queue<T>, T> popNext;
            public Action<Queue<T>, T> pushSuccs;
            public Action<Queue<T>> clear;
            public Action<Action<T>, T> pushSuccsUser;

            public static State Create(IObjectPool<State> pool) {
               var s = new State();
               s.pool = pool;
               s.store = new Queue<T>();
               s.popNext = store => store.Dequeue();
               Action<T> storeEnqueue = s.store.Enqueue;
               s.pushSuccs = (_, el) => s.pushSuccsUser(storeEnqueue, el);
               s.clear = store => store.Clear();
               s.pushSuccsUser = null;
               return s;
            }

            public void Dispose() {
               pool.ReturnObject(this);
            }
         }
      }

      public static EnumeratorToEnumerableAdapter<T, TraversalEnumeratorBase<T, Queue<T>>> Bfs<T>(this T root, Action<Action<T>, T> pushSuccsUser) {
         return BfsInternals<T>.Bfs(root, pushSuccsUser);
      }

      public static EnumeratorToEnumerableAdapter<T, TraversalEnumeratorBase<T, Stack<T>>> Dive<T>(this T root, Func<T, T> next) {
         return DfsInternals<T>.Dive(root, next);
      }
   }
}
