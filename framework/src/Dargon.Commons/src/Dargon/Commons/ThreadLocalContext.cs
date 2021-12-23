using System;
using System.Collections.Generic;
using System.Diagnostics;
using Dargon.Commons.Exceptions;

namespace Dargon.Commons {
   public class ThreadLocalContext<TContext> where TContext : ThreadLocalContext<TContext> {
      public TContext UseAsImplicitThreadLocalContext() {
         UseImplicitContext((TContext)this);
         return (TContext)this;
      }

      public PopContextOnDispose PushForScope() {
         return WithContextScope((TContext)this);
      }

      private static class Store<TUnique> where TUnique : struct {
         [ThreadStatic] public static State_t s_tlsState;

         public struct State_t {
            public bool IsInitialized;
            public Stack<TContext> ContextStack;
            public bool UseImplicitContext;
            public StackTrace InitializeThreadLocalStateStackTrace;
         }
      }

      private struct StructOf<T> {
         private T unused;
      }

      private static ref Store<StructOf<TContext>>.State_t GetState() => ref Store<StructOf<TContext>>.s_tlsState;

      public static void AssertThreadLocalStateIsInitialized() {
         GetState().IsInitialized.AssertIsTrue($"{nameof(ThreadLocalContext<TContext>)} has yet to be initialized.");
      }

      private static void InitializeThreadLocalState() {
         ref var state = ref GetState();
         if (state.IsInitialized) {
            return;
         }

         state.IsInitialized = true;
         state.ContextStack = new Stack<TContext>();
         state.UseImplicitContext = false;
         state.InitializeThreadLocalStateStackTrace = new StackTrace();
      }

      public static TContext CurrentContext => GetCurrentContext();
      
      private static TContext GetCurrentContext() {
         AssertThreadLocalStateIsInitialized();
         return GetState().ContextStack.Peek();
      }

      public static void UseImplicitContext(TContext ctx) {
         ref var state = ref GetState();
         
         if (state.IsInitialized) {
            throw new InvalidStateException($"{nameof(UseImplicitContext)} must be invoked prior to the first context push. Prior init at: ${state.InitializeThreadLocalStateStackTrace}");
         }

         InitializeThreadLocalState();

         state.ContextStack.Count.AssertEquals(0);
         state.ContextStack.Push(ctx);
         state.UseImplicitContext = true;
      }

      public static void PushContext(TContext ctx) {
         InitializeThreadLocalState();
         GetState().ContextStack.Push(ctx);
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