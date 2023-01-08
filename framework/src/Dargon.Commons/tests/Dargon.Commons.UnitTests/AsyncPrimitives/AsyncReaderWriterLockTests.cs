using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Dargon.Commons.AsyncPrimitives {
   public class AsyncReaderWriterLockTests {
      [Fact]
      public async Task SingleReaderHappyPath() {
         var arwl = new AsyncReaderWriterLock();
         await using var g1 = await arwl.CreateReaderGuardAsync();
         arwl.DebugReaderWriterDepth.AssertEquals((1, 0));
      }

      [Fact]
      public async Task SingleWriterHappyPath() {
         var arwl = new AsyncReaderWriterLock();
         await using var g1 = await arwl.CreateWriterGuardAsync();
         arwl.DebugReaderWriterDepth.AssertEquals((0, 1));
      }

      [Fact]
      public async Task ReentrantReaderHappyPath() {
         var arwl = new AsyncReaderWriterLock();
         await using var g1 = await arwl.CreateReaderGuardAsync();
         await using var g2 = await arwl.CreateReaderGuardAsync();
         arwl.DebugReaderWriterDepth.AssertEquals((2, 0));
      }

      [Fact]
      public async Task ReentrantWriterHappyPath1() {
         var arwl = new AsyncReaderWriterLock();
         await using var g1 = await arwl.CreateWriterGuardAsync();
         await using var g2 = await arwl.CreateWriterGuardAsync();
         arwl.DebugReaderWriterDepth.AssertEquals((0, 2));
      }

      [Fact]
      public async Task ReentrantWriterHappyPath2() {
         var arwl = new AsyncReaderWriterLock();
         await using var g1 = await arwl.CreateWriterGuardAsync();
         await using var g2 = await arwl.CreateReaderGuardAsync();
         arwl.DebugReaderWriterDepth.AssertEquals((0, 2));
      }

      [Fact]
      public async Task Complex() {
         var arwl = new AsyncReaderWriterLock();
         {
            await using var g1 = await arwl.CreateWriterGuardAsync();
            await using var g2 = await arwl.CreateReaderGuardAsync();
            arwl.DebugReaderWriterDepth.AssertEquals((0, 2));
         }

         {
            await using var g3 = await arwl.CreateReaderGuardAsync();
            await using var g4 = await arwl.CreateReaderGuardAsync();
            arwl.DebugReaderWriterDepth.AssertEquals((2, 0));
         }
      }

      [Fact]
      public async Task CreateWriterGuardAsync_FailsUnderNestedReaderLock() {
         var arwl = new AsyncReaderWriterLock();
         await using var g1 = await arwl.CreateReaderGuardAsync();
         try {
            await using var g2 = await arwl.CreateWriterGuardAsync();
            throw new Exception("Expected IOE");
         } catch (InvalidOperationException) {
            // success
         }
      }
   }
}
