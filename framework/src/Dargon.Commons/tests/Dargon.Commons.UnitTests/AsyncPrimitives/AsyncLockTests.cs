using System;
using System.Formats.Asn1;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static Dargon.Commons.Channels.ChannelsExtensions;

namespace Dargon.Commons.AsyncPrimitives {
   public class AsyncLockTests {
      [Fact]
      public async Task SingleLockHappyPath() {
         var al = new AsyncLock();
         al.DebugLockDepth.AssertEquals(0);
         {
            using var l1 = await al.LockAsync();
            al.DebugLockDepth.AssertEquals(1);
         }
         al.DebugLockDepth.AssertEquals(0);
      }

      [Fact]
      public async Task DoubleLockHappyPath() {
         var al = new AsyncLock();
         al.DebugLockDepth.AssertEquals(0);
         {
            using var l1 = await al.LockAsync();
            al.DebugLockDepth.AssertEquals(1);
            {
               al.DebugLockDepth.AssertEquals(1);
               using var l2 = await al.LockAsync();
               al.DebugLockDepth.AssertEquals(2);
            }
            al.DebugLockDepth.AssertEquals(1);
         }
         al.DebugLockDepth.AssertEquals(0);
      }

      [Fact]
      public async Task LockAsyncUnsafe_WithImportantCaveats_Test() {
         var al = new AsyncLock();
         var lockTakenSignal = new AsyncLatch();
         var lockShouldBeFreedSignal = new AsyncLatch();

         al.DebugLockDepth.AssertEquals(0);

         // Create co-running task described below
         var t = Go(async () => {
            al.DebugLockDepth.AssertEquals(0);
            {
               using var g1 = await al.LockAsync();
               al.DebugLockDepth.AssertEquals(1);
               lockTakenSignal.SetOrThrow();
               al.DebugLockDepth.AssertEquals(1);
               await lockShouldBeFreedSignal.WaitAsync();
               al.DebugLockDepth.AssertEquals(1);
            }
            al.DebugLockDepth.AssertEquals(0);
         });

         // Part 1: Have the co-task take the lock. Verify timeout throws and counter behaves as expected.
         al.DebugLockDepth.AssertEquals(0);
         await lockTakenSignal.WaitAsync();
         al.DebugLockDepth.AssertEquals(0);

         try {
            al.DebugLockDepth.AssertEquals(0);
            using var g2 = await al.LockAsyncUnsafe_WithImportantCaveats(new CancellationTokenSource(100).Token);
            throw new Exception("This should't be hit");
         } catch (OperationCanceledException) {
            al.DebugLockDepth.AssertEquals(1);
            al.NotifyThatLockAsyncUnsafeThrew();
            al.DebugLockDepth.AssertEquals(0);
         }

         // Part 2: Have the co-task free the lock. Verify we can now take it.
         al.DebugLockDepth.AssertEquals(0);

         lockShouldBeFreedSignal.SetOrThrow();

         al.DebugLockDepth.AssertEquals(0);

         await t;

         al.DebugLockDepth.AssertEquals(0);
         {
            al.DebugLockDepth.AssertEquals(0);
            using var g3 = await al.LockAsync();
            al.DebugLockDepth.AssertEquals(1);
         }
         al.DebugLockDepth.AssertEquals(0);
      }
   }
}
