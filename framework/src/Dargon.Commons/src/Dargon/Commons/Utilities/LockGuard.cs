using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

   public enum GuardState {
      UpgradeableReader,
      UpgradedWriter,
      SimpleReader,
      Disposed,
      Intermediate_ReaderToUpgradable,
      Intermediate_ReaderToWriter,
   }

   /// <summary>
   /// All synchronization methods, including disposal, contain a full memory barrier.
   /// This object gracefully handles double disposal.
   /// </summary>
   public struct RWLSUpgradableReaderGuard : IDisposable {
      private readonly ReaderWriterLockSlim rwl;
      private GuardState state;

      public RWLSUpgradableReaderGuard(ReaderWriterLockSlim rwl, GuardState state = GuardState.UpgradeableReader) {
         this.rwl = rwl;
         this.state = state;

         switch (state) {
            case GuardState.UpgradeableReader:
               rwl.EnterUpgradeableReadLock();
               break;
            case GuardState.UpgradedWriter:
               rwl.EnterWriteLock();
               break;
            case GuardState.SimpleReader:
               rwl.EnterReadLock();
               break;
            default:
               throw new ArgumentException($"Unexpected {nameof(state)} = ({nameof(GuardState)}){state}");
         }
      }

      public bool IsUpgradeableReader => state == GuardState.UpgradeableReader;
      public bool IsUpgradedWriter => state == GuardState.UpgradedWriter;
      public bool IsSimpleReader => state == GuardState.SimpleReader;
      public bool IsDisposed => state == GuardState.Disposed;

      // public bool IsReaderLock => isUpgradableReaderLockElseWriterLock;
      // public bool IsUpgradedWriterLock => !isUpgradableReaderLockElseWriterLock;

      public void UpgradeToOrStayAsWriterLock() {
         if (state == GuardState.UpgradedWriter) return;
         UpgradeToWriterLock();
      }

      public void UpgradeToWriterLock() {
         state.AssertEquals(GuardState.UpgradeableReader);

         rwl.EnterWriteLock();
         rwl.ExitUpgradeableReadLock();
         state = GuardState.UpgradedWriter;
      }

      public void DowngradeToUpgradeableReaderLock() {
         state.AssertEquals(GuardState.UpgradedWriter);

         rwl.EnterUpgradeableReadLock();
         rwl.ExitWriteLock();

         state = GuardState.UpgradeableReader;
      }

      public void DowngradeToReaderLock() {
         state.AssertNotEquals(GuardState.SimpleReader);
         state.AssertNotEquals(GuardState.Disposed);

         if (state == GuardState.UpgradedWriter) {
            rwl.EnterReadLock();
            rwl.ExitWriteLock();
         } else if (state == GuardState.UpgradeableReader) {
            rwl.EnterReadLock();
            rwl.ExitUpgradeableReadLock();
         }

         state = GuardState.SimpleReader;
      }

      public void Dispose() {
         switch (state) {
            case GuardState.UpgradeableReader:
               rwl.ExitUpgradeableReadLock();
               break;
            case GuardState.UpgradedWriter:
               rwl.ExitWriteLock();
               break;
            case GuardState.SimpleReader:
               rwl.ExitReadLock();
               break;
            case GuardState.Disposed:
               break;
         }

         state = GuardState.Disposed;
      }

      public void ReleaseReaderAndReacquireAsUpgradableReader() {
         state.AssertEquals(GuardState.SimpleReader);
         rwl.ExitReadLock();
         state = GuardState.Intermediate_ReaderToUpgradable;
         rwl.EnterUpgradeableReadLock();
         state = GuardState.UpgradeableReader;
      }

      public void ReleaseReaderAndReacquireAsUpgradedWriter() {
         state.AssertEquals(GuardState.SimpleReader);
         rwl.ExitReadLock();
         state = GuardState.Intermediate_ReaderToWriter;
         rwl.EnterWriteLock();
         state = GuardState.UpgradedWriter;
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

   public static class RWLSExtensions {
      public static RWLSReaderGuard CreateReaderGuard(this ReaderWriterLockSlim rwls) => new(rwls);
      public static RWLSUpgradableReaderGuard CreateUpgradableReaderGuard(this ReaderWriterLockSlim rwls) => new(rwls);
      public static RWLSUpgradableReaderGuard CreateUpgradableReaderGuard(this ReaderWriterLockSlim rwls, GuardState guardState) => new(rwls, guardState);
      public static RWLSWriterGuard CreateWriterGuard(this ReaderWriterLockSlim rwls) => new(rwls);

      public static bool TryGetValueWithDoubleCheckedLock<TKey, TValue>(this Dictionary<TKey, TValue> d, TKey key, out TValue value, RWLSUpgradableReaderGuard guard) {
         if (d.TryGetValue(key, out value)) {
            return true;
         }

         guard.UpgradeToWriterLock();

         return d.TryGetValue(key, out value);
      }

      public static bool TryGetValueWithDoubleCheckedLock<TKey, TValue>(this ExposedListDictionary<TKey, TValue> d, TKey key, out TValue value, RWLSUpgradableReaderGuard guard) {
         if (d.TryGetValue(key, out value)) {
            return true;
         }

         guard.UpgradeToWriterLock();

         return d.TryGetValue(key, out value);
      }
   }
}
