using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Courier;
using Dargon.Hydrous.Impl;
using Dargon.Ryu;
using Dargon.Vox;
using NMockito;
using Xunit;

namespace Dargon.Hydrous {
   public class EntryTests : NMockitoInstance {
      [Fact]
      public void RoundTripCloneTest() {
         // instantiate ryu to load vox types which has entry typeid
         new RyuFactory().Create();

         var threadCount = 8;
         var startSync = new CountdownEvent(threadCount);
         for (var thread = 0; thread < threadCount; thread++) {
            new Thread(() => {
               startSync.Signal();
               startSync.Wait();

               for (var i = 0; i < 10000; i++) {
                  var entry = Entry<int, string>.Create(123);
                  entry.Value = "" + ('a' + i % 26);

                  var clone = entry.DeepCloneSerializable();

                  AssertEquals(entry.Key, clone.Key);
                  AssertEquals(entry.Value, clone.Value);
                  AssertEquals(entry.Exists, clone.Exists);
                  AssertEquals(entry.IsDirty, clone.IsDirty);
               }
            }).Start();
         }
      }
   }
}