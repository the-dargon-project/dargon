using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
         System.Threading.Monitor.Enter(lockObj, ref lockWasTaken);
      }

      public void Dispose() {
         if (isDisposed) {
            return;
         }

         isDisposed = true;

         if (lockWasTaken) {
            System.Threading.Monitor.Exit(lockObj);
         }
      }
   }

   /// <summary>
   /// All synchronization methods, including disposal, contain a full memory barrier.
   /// This object gracefully handles double disposal.
   /// </summary>
   public struct RWLSReaderGuard : IDisposable {
      private readonly ReaderWriterLockSlim rwl;
      private readonly bool lockWasTaken;
      private bool isDisposed;

      public RWLSReaderGuard(ReaderWriterLockSlim rwl) {
         this.rwl = rwl;
         this.lockWasTaken = false;
         this.isDisposed = false;
         rwl.EnterReadLock();
         lockWasTaken = true;
      }

      public void Dispose() {
         if (isDisposed) {
            return;
         }

         isDisposed = true;

         if (lockWasTaken) {
            rwl.ExitReadLock();
         }
      }
   }

   /// <summary>
   /// All synchronization methods, including disposal, contain a full memory barrier.
   /// This object gracefully handles double disposal.
   /// </summary>
   public struct RWLSUpgradableReaderGuard : IDisposable {
      private readonly ReaderWriterLockSlim rwl;
      private bool isDisposed;
      private bool isUpgradableReaderLockElseWriterLock;

      public RWLSUpgradableReaderGuard(ReaderWriterLockSlim rwl) {
         this.rwl = rwl;
         this.isDisposed = false;
         this.isUpgradableReaderLockElseWriterLock = true;
         rwl.EnterUpgradeableReadLock();
      }

      public void UpgradeToWriterLock() {
         rwl.EnterWriteLock();
         this.isUpgradableReaderLockElseWriterLock = false;
      }

      public void Dispose() {
         if (isDisposed) {
            return;
         }

         isDisposed = true;

         if (isUpgradableReaderLockElseWriterLock) {
            rwl.ExitUpgradeableReadLock();
         } else {
            rwl.ExitWriteLock();
         }
      }
   }

   /// <summary>
   /// All synchronization methods, including disposal, contain a full memory barrier.
   /// This object gracefully handles double disposal.
   /// </summary>
   public struct RWLSWriterGuard : IDisposable {
      private readonly ReaderWriterLockSlim rwl;
      private readonly bool lockWasTaken;
      private bool isDisposed;

      public RWLSWriterGuard(ReaderWriterLockSlim rwl) {
         this.rwl = rwl;
         this.lockWasTaken = false;
         this.isDisposed = false;
         rwl.EnterWriteLock();
         lockWasTaken = true;
      }

      public void Dispose() {
         if (isDisposed) {
            return;
         }

         isDisposed = true;

         if (lockWasTaken) {
            rwl.ExitWriteLock();
         }
      }
   }
}
