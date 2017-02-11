using Dargon.Ryu;
using NMockito;
using System.Threading;

namespace Dargon.Vox.FunctionalTests.RoundTripTests {
   public class GlobalSerializerTests : NMockitoInstance {
      public void Run() {
         // instantiate ryu to load vox types which has entry typeid
         new RyuFactory().Create();

         var threadCount = 8;
         var startSync = new CountdownEvent(threadCount);
         for (var thread = 0; thread < threadCount; thread++) {
            new Thread(() => {
               startSync.Signal();
               startSync.Wait();

               for (var i = 0; i < 10000; i++) {
                  var original = new Box<int> { Value = i };
                  var clone = original.DeepCloneSerializable();

                  AssertTrue(original != clone);
                  AssertEquals(original.Value, clone.Value);
               }
            }).Start();
         }
      }

      public class CustomVoxTypes : VoxTypes {
         public CustomVoxTypes() : base(100000) {
            Register(0, typeof(Box<>));
         }
      }

      [AutoSerializable]
      public class Box<T> {
         public T Value { get; set; }
      }
   }
}
