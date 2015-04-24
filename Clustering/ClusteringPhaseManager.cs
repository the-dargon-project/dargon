﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Services.Server;
using Nito.AsyncEx;

namespace Dargon.Services.Clustering {
   public interface ClusteringPhaseManager : IDisposable {
      void Transition(ClusteringPhase nextClusteringPhase);
      void HandleServiceRegistered(InvokableServiceContext invokableServiceContext);
      void HandleServiceUnregistered(InvokableServiceContext invokableServiceContext);

      Task<object> InvokeServiceCall(Guid serviceGuid, string methodName, object[] methodArguments);
   }

   public class ClusteringPhaseManagerImpl : ClusteringPhaseManager {
      private readonly ReaderWriterLockSlim synchronization = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
      private bool disposed = false;
      private ClusteringPhase currentClusteringPhase = null;

      public void Transition(ClusteringPhase nextClusteringPhase) {
         synchronization.EnterWriteLock();
         try {
            ThrowIfDisposed();

            Debug.WriteLine("Transition from phase " + (currentClusteringPhase == null ? "null" : currentClusteringPhase.ToString()) + " to " + (nextClusteringPhase == null ? "null" : nextClusteringPhase.ToString()));
            currentClusteringPhase = nextClusteringPhase;
            currentClusteringPhase.HandleEnter();
         } finally {
            synchronization.ExitWriteLock();
         }
      }

      public void HandleServiceRegistered(InvokableServiceContext invokableServiceContext) {
         synchronization.EnterReadLock();
         try {
            currentClusteringPhase.HandleServiceRegistered(invokableServiceContext);
         } finally {
            synchronization.ExitReadLock();
         }
      }

      public void HandleServiceUnregistered(InvokableServiceContext invokableServiceContext) {
         synchronization.EnterReadLock();
         try {
            currentClusteringPhase.HandleServiceUnregistered(invokableServiceContext);
         } finally {
            synchronization.ExitReadLock();
         }
      }

      public Task<object> InvokeServiceCall(Guid serviceGuid, string methodName, object[] methodArguments) {
         synchronization.EnterReadLock();
         try {
            return currentClusteringPhase.InvokeServiceCall(serviceGuid, methodName, methodArguments);
         } finally {
            synchronization.ExitReadLock();
         }
      }

      public void Dispose() {
         synchronization.EnterWriteLock();
         try {
            if (!disposed) {
               disposed = true;
               currentClusteringPhase.Dispose();
            }
         } finally {
            synchronization.ExitWriteLock();
         }
      }

      internal void ThrowIfDisposed() {
         if (disposed) {
            const string error = "This connector context has already been disposed!";
            throw new ObjectDisposedException("this", error);
         }
      }
   }
}
