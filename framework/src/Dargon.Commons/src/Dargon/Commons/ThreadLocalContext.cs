using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.Exceptions;

namespace Dargon.Commons {
   public class ThreadLocalContext<TContext> where TContext : ThreadLocalContext<TContext> {
      public TContext UseAsImplicitThreadLocalContext(bool reinitialize = false) {
         if (reinitialize) {
            UninitializeState();
         }
         UseImplicitThreadLocalContext((TContext)this);
         return (TContext)this;
      }

      public TContext UseAsImplicitAsyncLocalContext(bool reinitialize = false) {
         if (reinitialize) {
            UninitializeState();
         }
         UseImplicitAsyncLocalContext((TContext)this);
         return (TContext)this;
      }

      public PopContextOnDispose PushForScope() {
         return WithContextScope((TContext)this);
      }

      public PopContextOnDispose PushForScopeAsImplicitThreadLocalContextIfUninitialized() {
         return WithContextScopeAndImplicitThreadLocalContextIfUninitialized((TContext)this);
      }

      public PopContextOnDispose PushForScopeAsImplicitAsyncLocalContextIfUninitialized() {
         return WithContextScopeAndImplicitAsyncLocalContextIfUninitialized((TContext)this);
      }

      private static class Store<TUnique> where TUnique : struct {
         [ThreadStatic] public static State_t s_tlsState;
         public static AsyncLocal<Box<State_t>> s_alsState = new();

         public class Box<T> {
            public T Value;
         }

         public static bool IsInitialized => s_tlsState.IsInitialized || (s_alsState.Value?.Value.IsInitialized ?? false);

         public static ref State_t GetStateRef(bool? preferTlsElseAls) {
            if (s_alsState.Value is { } alsBox) {
               return ref alsBox.Value;
            } else if (s_tlsState.IsInitialized) {
               return ref s_tlsState;
            } else if (!preferTlsElseAls.HasValue) {
               throw new InvalidStateException($"{nameof(preferTlsElseAls)} was null. Have you initialized the thread/async-local context?");
            } else if (preferTlsElseAls.Value) {
               return ref s_tlsState;
            } else {
               var box = s_alsState.Value = new Box<State_t>();
               return ref box.Value;
            }
         }

         public struct State_t {
            public bool IsInitialized;
            public Stack<TContext> ContextStack;
            public bool UseImplicitContext;
            public StackTrace InitializeStateStackTrace;
         }
      }

      private struct StructOf<T> {
         private T unused;
      }

      private const bool kPreferThreadLocalState = true;
      private const bool kPreferAsyncLocalState = false;

      private static void UninitializeState() {
         Store<StructOf<TContext>>.s_alsState.Value = null;
         Store<StructOf<TContext>>.s_tlsState = default;
      }

      private static bool IsStateInitialized => Store<StructOf<TContext>>.IsInitialized;

      private static ref Store<StructOf<TContext>>.State_t GetState(bool? preferTlsElseAls = null) => ref Store<StructOf<TContext>>.GetStateRef(preferTlsElseAls); 
      
      private static ref Store<StructOf<TContext>>.State_t GetAlreadyInitializedState() {
         ref var state = ref Store<StructOf<TContext>>.GetStateRef(null);
         state.IsInitialized.AssertIsTrue($"Invoke {nameof(InitializeThreadLocalState)} or {nameof(InitializeAsyncLocalState)} first!");
         return ref state;
      }

      public static void AssertThreadAndAsyncLocalStateAreNotInitialized() {
         Store<StructOf<TContext>>.s_tlsState.IsInitialized.AssertIsFalse();
         Store<StructOf<TContext>>.s_alsState.Value.AssertIsNull();
      }

      public static void AssertThreadOrAsyncLocalStateIsInitialized() {
         GetState().IsInitialized.AssertIsTrue($"{nameof(ThreadLocalContext<TContext>)} has yet to be initialized.");
      }

      public static void InitializeThreadLocalState() => InitializeStateInternal(true);
      
      public static void InitializeAsyncLocalState() => InitializeStateInternal(false);

      private static void InitializeStateInternal(bool preferThreadElseAsyncLocalState) {
         ref var state = ref GetState(preferThreadElseAsyncLocalState);
         if (state.IsInitialized) {
            throw new InvalidOperationException("State is already initialized.");
         }

         state.IsInitialized = true;
         state.ContextStack = new Stack<TContext>();
         state.UseImplicitContext = false;
         state.InitializeStateStackTrace = new StackTrace();
      }

      public static TContext CurrentContext => GetCurrentContext();
      
      private static TContext GetCurrentContext() {
         AssertThreadOrAsyncLocalStateIsInitialized();
         return GetState().ContextStack.Peek();
      }

      public static void UseImplicitThreadLocalContext(TContext ctx) => HighLevelInitializeStateWithContext(ctx, true, true);

      public static void UseImplicitAsyncLocalContext(TContext ctx) => HighLevelInitializeStateWithContext(ctx, false, true);

      private static void HighLevelInitializeStateWithContext(TContext ctx, bool preferThreadElseAsyncLocalContext, bool useImplicitContext) {
         ref var state = ref GetState(preferThreadElseAsyncLocalContext);
         
         if (state.IsInitialized) {
            throw new InvalidStateException($"{nameof(UseImplicitThreadLocalContext)} must be invoked prior to the first context push. Prior init at: ${state.InitializeStateStackTrace}");
         }

         InitializeStateInternal(preferThreadElseAsyncLocalContext);

         state.ContextStack.Count.AssertEquals(0);
         state.ContextStack.Push(ctx);
         state.UseImplicitContext = useImplicitContext;
      }

      public static void PushContext(TContext ctx) {
         GetAlreadyInitializedState().ContextStack.Push(ctx);
      }

      public static void PopContext() {
         ref var state = ref GetState();
         if (!state.IsInitialized) {
            throw new InvalidStateException($"Attempted to ${nameof(PopContext)} prior to TLS init!?");
         }

         // if popping to an empty stack, then uninitialize the thread-local state.
         // this is ideally only possible via WithContextScopeAndImplicitThreadLocalContextIfUninitialized
         var minInitialStackSize = state.UseImplicitContext ? 2 : 1;
         state.ContextStack.Count.AssertIsGreaterThanOrEqualTo(minInitialStackSize);
         state.ContextStack.Pop();

         if (!state.UseImplicitContext && state.ContextStack.Count == 0) {
            UninitializeState();
         }
      }

      public static PopContextOnDispose WithContextScope(TContext context) {
         PushContext(context);
         return new PopContextOnDispose();
      }

      public static PopContextOnDispose WithContextScopeAndImplicitThreadLocalContextIfUninitialized(TContext ctx) {
         if (IsStateInitialized) {
            PushContext(ctx);
         } else {
            HighLevelInitializeStateWithContext(ctx, true, false);
         }
         return new PopContextOnDispose();
      }

      public static PopContextOnDispose WithContextScopeAndImplicitAsyncLocalContextIfUninitialized(TContext ctx) {
         if (IsStateInitialized) {
            PushContext(ctx);
         } else {
            HighLevelInitializeStateWithContext(ctx, false, false);
         }
         return new PopContextOnDispose();
      }

      public struct PopContextOnDispose : IDisposable {
         public void Dispose() => PopContext();
      }
   }
}