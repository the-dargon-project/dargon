using System;
using Dargon.Vox.RoundTripTests;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Xunit;

namespace Dargon.Vox.FunctionalTests.RoundTripTests {
   public class AutoSerializableRoundTripTest1 : RoundTripTest {
      private static readonly Type[] kTestTypes = { typeof(Box<>) };

      public AutoSerializableRoundTripTest1() : base(kTestTypes) { }

      [Fact]
      public void EasyTest() {
         var subject = new Box<string> { Value = "Xwth" };
         MultiThreadedRoundTripTest(new object[] { subject }, 1000, 8);
      }

      [Fact]
      public void HardTest1() {
         var subject = new Box<Box<string>> { Value = new Box<string> { Value = "Xwth" } };
         MultiThreadedRoundTripTest(new object[] { subject }, 1000, 8);
      }

      [Fact]
      public void HardTest2() {
         var subject = new Box<ConcurrentDictionary<string, HashSet<Box<string>>>> {
            Value = new ConcurrentDictionary<string, HashSet<Box<string>>> {
               ["Xwth"] = new HashSet<Box<string>> {
                  new Box<string> { Value = "asdf" },
                  new Box<string> { Value = "jkl" },
                  new Box<string> { Value = "qwerty" }
               }
            }
         };
         MultiThreadedRoundTripTest(
            new [] { subject }, 
            1000, 
            8,
            (expected, actual) => {
               AssertCollectionDeepEquals(expected.Value, actual.Value);
            });
      }

      [AutoSerializable]
      public class Box<T> {
         public T Value { get; set; }

         public override int GetHashCode() => Value?.GetHashCode() ?? 0;

         public override bool Equals(object obj) {
            var box = obj as Box<T>;
            return box != null && Value.Equals(box.Value);
         }
      }
   }
}
