using System;
using System.Linq;
using System.Threading.Tasks;

namespace NMockito.Expectations {
   public static class ExpectationExtensions {
      public static Expectation ThenResolve(this Expectation e, params object[] values) {
         return e.ThenReturn(_MapHelper(values, x => (object)Task.FromResult(x)));
      }

      public static Expectation<Task<TReturnValue>> ThenResolve<TReturnValue>(this Expectation<Task<TReturnValue>> e, params TReturnValue[] values) {
         return e.ThenReturn(_MapHelper(values, Task.FromResult));
      }

      public static Expectation<TOut1, Task<TReturnValue>> ThenResolve<TOut1, TReturnValue>(this Expectation<TOut1, Task<TReturnValue>> e, TReturnValue value) {
         return e.ThenReturn(Task.FromResult(value));
      }

      public static Expectation<TOut1, TOut2, Task<TReturnValue>> ThenResolve<TOut1, TOut2, TReturnValue>(this Expectation<TOut1, TOut2, Task<TReturnValue>> e, TReturnValue value) {
         return e.ThenReturn(Task.FromResult(value));
      }

      public static Expectation<TOut1, TOut2, TOut3, Task<TReturnValue>> ThenResolve<TOut1, TOut2, TOut3, TReturnValue>(this Expectation<TOut1, TOut2, TOut3, Task<TReturnValue>> e, TReturnValue value) {
         return e.ThenReturn(Task.FromResult(value));
      }

      public static Expectation ThenReject(this Expectation e, params Exception[] exceptions) {
         return e.ThenReturn(_MapHelper(exceptions, x => (object)Task.FromException(x)));
      }

      public static Expectation<Task<TReturnValue>> ThenReject<TReturnValue>(this Expectation<Task<TReturnValue>> e, params Exception[] exceptions) {
         return e.ThenReturn(_MapHelper(exceptions, Task.FromException<TReturnValue>));
      }
      public static Expectation<TOut1, Task<TReturnValue>> ThenReject<TOut1, TReturnValue>(this Expectation<TOut1, Task<TReturnValue>> e, params Exception[] exceptions) {
         return exceptions.Aggregate(e, (current, ex) => current.ThenReturn(Task.FromException<TReturnValue>(ex)));
      }

      public static Expectation<TOut1, TOut2, Task<TReturnValue>> ThenReject<TOut1, TOut2, TReturnValue>(this Expectation<TOut1, TOut2, Task<TReturnValue>> e, params Exception[] exceptions) {
         return exceptions.Aggregate(e, (current, ex) => current.ThenReturn(Task.FromException<TReturnValue>(ex)));
      }

      public static Expectation<TOut1, TOut2, TOut3, Task<TReturnValue>> ThenReject<TOut1, TOut2, TOut3, TReturnValue>(this Expectation<TOut1, TOut2, TOut3, Task<TReturnValue>> e, params Exception[] exceptions) {
         return exceptions.Aggregate(e, (current, ex) => current.ThenReturn(Task.FromException<TReturnValue>(ex)));
      }

      private static R[] _MapHelper<T, R>(T[] items, Func<T, R> project) {
         var r = new R[items.Length];
         for (var i = 0; i < r.Length; i++) {
            r[i] = project(items[i]);
         }
         return r;
      }
   }
}