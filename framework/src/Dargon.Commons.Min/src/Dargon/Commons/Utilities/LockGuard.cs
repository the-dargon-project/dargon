using System;
using System.Collections.Generic;
using System.Threading;
using Dargon.Commons.Collections;

namespace Dargon.Commons.Utilities {
   /// <summary>
   /// All synchronization methods, including disposal, contain a full memory barrier.
   /// This object gracefully handles double disposal.
   /// </summary>
   public struct LockGuard : IDisposable {
      private readonly object lockObj;
      private readonly bool lockWasTaken;
      private bool isDisposed;

      public LockGuard(object lockObj) {
         this.lockObj = lockObj;
         this.lockWasTaken = false;
         this.isDisposed = false;
         Monitor.Enter(lockObj, ref lockWasTaken);
      }

      public void Dispose() {
         if (isDisposed) {
            return;
         }

         isDisposed = true;

         if (lockWasTaken) {
            Monitor.Exit(lockObj);
         }
      }
   }

   public static class LockGuardExtensions {
      public static LockGuard CreateLockGuard(this object o) => new(o);
   }
}
