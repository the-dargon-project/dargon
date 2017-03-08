using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Channels;
using NMockito;
using Xunit;
using Xunit.Abstractions;
using static Dargon.Commons.Channels.ChannelsExtensions;

namespace Dargon.Channels.Tests {
   public class ChannelSelectIT : NMockitoInstance {
      private readonly ITestOutputHelper output;

      public ChannelSelectIT(ITestOutputHelper output) {
         this.output = output;
      }

      [Fact]
      public async Task RunAsync() {
         var sw = new Stopwatch();
         for (var i = 0; i < 10; i++) {
            output.WriteLine($"Trial {i}: Entering.");
            sw.Restart();
            await RunTrialAsync();
            output.WriteLine($"Trial {i}: {sw.ElapsedMilliseconds} millis.");
         }
      }
      public async Task RunTrialAsync() {
         var channel1 = ChannelFactory.Nonblocking<int>();
         var channel2 = ChannelFactory.Blocking<int>();
         var barrier = new AsyncBarrier(2);
         var semaphore = new AsyncSemaphore(0);
         Go(async () => {
            for (var i = 0; i < 10; i++) {
//               await barrier.SignalAndWaitAsync();
//               await semaphore.WaitAsync();
//               await channel1.WriteAsync(i);
            }
         }).Forget();
         Go(async () => {
            for (var i = 0; i < 10; i++) {
               //await barrier.SignalAndWaitAsync();
               await semaphore.WaitAsync();
               await channel2.WriteAsync(i);
               await semaphore.WaitAsync();
               await channel1.WriteAsync(i);
            }
         }).Forget();
         int counter1 = 0;
         int counter2 = 0;
         for (var i = 0; i < 10; i++) {
            output.WriteLine("IT " + i);
            semaphore.Release(2);
            for (var j = 0; j < 2; j++) {
               await Select.Case(channel1, val => {
                  AssertEquals(counter1, val);
                  counter1++;
               }).Case(channel2, val => {
                  AssertEquals(counter2, val);
                  counter2++;
               }).WaitAsync();
            }
         }
      }
   }
}
