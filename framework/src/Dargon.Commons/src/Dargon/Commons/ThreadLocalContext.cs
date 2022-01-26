using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.Exceptions;

namespace Dargon.Commons {
   public class ThreadLocalContext<TContext> where TContext : ThreadLocalContext<TContext> {
      public TContext UseAsImplicitThreadLocalContext() {
         UseImplicitThreadLocalContext((TContext)this);
         return (TContext)this;
      }

      public TContext UseAsImplicitAsyncLocalContext() {
         UseImplicitAsyncLocalContext((TContext)this);
         return (TContext)this;
      }

      public PopContextOnDispose PushForScope() {
         return WithContextScope((TContext)this);
      }

      private static class Store<TUnique> where TUnique : struct {
         [ThreadStatic] public static State_t s_tlsState;
         public static AsyncLocal<Box<State_t>> s_alsState = new();

         public class Box<T> {
            public T Value;
         }

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
      
      private static ref Store<StructOf<TContext>>.State_t GetState(bool? preferTlsElseAls = null) => ref Store<StructOf<TContext>>.GetStateRef(preferTlsElseAls); 
      
      private static ref Store<StructOf<TContext>>.State_t GetAlreadyInitializedState() {
         ref var state = ref Store<StructOf<TContext>>.GetStateRef(null);
         state.IsInitialized.AssertIsTrue($"Invoke {nameof(InitializeThreadLocalState)} or {nameof(InitializeAsyncLocalState)} first!");
         return ref state;
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

      public static void UseImplicitThreadLocalContext(TContext ctx) => UseImplicitContext(ctx, true);

      public static void UseImplicitAsyncLocalContext(TContext ctx) => UseImplicitContext(ctx, false);

      private static void UseImplicitContext(TContext ctx, bool preferThreadElseAsyncLocalContext) {
         ref var state = ref GetState(preferThreadElseAsyncLocalContext);
         
         if (state.IsInitialized) {
            throw new InvalidStateException($"{nameof(UseImplicitThreadLocalContext)} must be invoked prior to the first context push. Prior init at: ${state.InitializeStateStackTrace}");
         }

         InitializeStateInternal(preferThreadElseAsyncLocalContext);

         state.ContextStack.Count.AssertEquals(0);
         state.ContextStack.Push(ctx);
         state.UseImplicitContext = true;
      }

      public static void PushContext(TContext ctx) {
         GetAlreadyInitializedState().ContextStack.Push(ctx);
      }

      public static void PopContext() {
         ref var state = ref GetState();
         if (!state.IsInitialized) {
            throw new InvalidStateException($"Attempted to ${nameof(PopContext)} prior to TLS init!?");
         }

         // can't pop an empty stack.
         var minInitialStackSize = state.UseImplicitContext ? 2 : 1;
         state.ContextStack.Count.AssertIsGreaterThanOrEqualTo(minInitialStackSize);
         state.ContextStack.Pop();
      }

      public static PopContextOnDispose WithContextScope(TContext context) {
         PushContext(context);
         return new PopContextOnDispose();
      }

      public struct PopContextOnDispose : IDisposable {
         public void Dispose() => PopContext();
      }
   }
}