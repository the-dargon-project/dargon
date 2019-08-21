using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dargon.Commons {
   public static class TaskExtensions {
      public static void Forget(this Task task) {
         task.Catch<Exception>(ex => {
            Console.WriteLine("Forgotten task threw: " + ex);
         }).Noop();
      }

      public static async Task Forgettable(this Task task) {
         await task.ConfigureAwait(false);
      }

      public static T Tap<T>(this T self, Action<T> func) {
         func(self);
         return self;
      }


      public static IEnumerable<T> TapEach<T>(this IEnumerable<T> self, Action<T> func) {
         foreach (var x in self) {
            func(x);
            yield return x;
         }
      }

      public static async Task Tap(this Task self, Action func) {
         await self;
         func();
      }

      public static async Task Tap<T>(this Task self, Func<Task> func) {
         await self;
         await func();
      }

      public static async Task<T> Tap<T>(this Task<T> self, Action<T> func) {
         var result = await self;
         func(result);
         return result;
      }

      public static async Task<T> Tap<T>(this Task<T> self, Func<T, Task> func) {
         var result = await self;
         await func(result);
         return result;
      }

      public static U Then<T, U>(this T self, Func<T, U> func) {
         return func(self);
      }

      public static async Task<U> Then<U>(this Task self, Func<U> func) {
         await self;
         return func();
      }

      public static async Task<U> Then<U>(this Task self, Func<Task<U>> func) {
         await self;
         return await func();
      }

      public static async Task<U> Then<T, U>(this Task<T> self, Func<T, U> func) {
         return func(await self);
      }

      public static async Task<U> Then<T, U>(this Task<T> self, Func<T, Task<U>> func) {
         return await func(await self);
      }

      public static async Task Catch(this Task task, Action func) {
         try {
            await task;
         } catch {
            func();
         }
      }

      public static async Task Catch<TException>(this Task task, Action func) where TException : Exception {
         try {
            await task;
         } catch (TException) {
            func();
         }
      }

      public static async Task Catch<TException>(this Task task, Action<TException> func) where TException : Exception {
         try {
            await task;
         } catch (TException ex) {
            func(ex);
         }
      }

      public static async Task Catch(this Task task, Func<Task> func) {
         try {
            await task;
         } catch {
            await func();
         }
      }

      public static async Task Catch<TException>(this Task task, Func<Task> func) where TException : Exception {
         try {
            await task;
         } catch (TException) {
            await func();
         }
      }

      public static async Task Catch<TException>(this Task task, Func<TException, Task> func) where TException : Exception {
         try {
            await task;
         } catch (TException ex) {
            await func(ex);
         }
      }

      public static async Task<T> Catch<T>(this Task<T> task, Func<T> func) {
         try {
            return await task;
         } catch {
            return func();
         }
      }

      public static async Task<T> Catch<T, TException>(this Task<T> task, Func<T> func) where TException : Exception {
         try {
            return await task;
         } catch (TException) {
            return func();
         }
      }

      public static async Task<T> Catch<T, TException>(this Task<T> task, Func<TException, T> func) where TException : Exception {
         try {
            return await task;
         } catch (TException ex) {
            return func(ex);
         }
      }

      public static async Task<T> Catch<T>(this Task<T> task, Func<Task<T>> func) {
         try {
            return await task;
         } catch {
            return await func();
         }
      }

      public static async Task<T> Catch<T, TException>(this Task<T> task, Func<Task<T>> func) where TException : Exception {
         try {
            return await task;
         } catch (TException) {
            return await func();
         }
      }

      public static async Task<T> Catch<T, TException>(this Task<T> task, Func<TException, Task<T>> func) where TException : Exception {
         try {
            return await task;
         } catch (TException ex) {
            return await func(ex);
         }
      }
   }
}