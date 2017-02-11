using Dargon.Vox.RoundTripTests;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Xunit;

namespace Dargon.Vox.FunctionalTests.RoundTripTests {
   public class DictionaryLikeRoundtripFT : RoundTripTest {
      private static readonly Dictionary<string, Type> kEasyTestData = new Dictionary<string, Type> {
         ["asdf"] = typeof(int),
         ["jkl"] = typeof(string)
      };

      private static readonly Dictionary<string, Dictionary<string, Type[]>[]> kHardTestData1 = new Dictionary<string, Dictionary<string, Type[]>[]> {
         ["hailviral1"] = new[] {
            new Dictionary<string, Type[]> {
               ["kadence696"] = new [] { typeof(int), typeof(string), typeof(bool) },
               ["Flumpywumpy123"] = new [] { typeof(int), typeof(bool), typeof(string) }
            },
         },
         ["jkl;"] = new[] {
            new Dictionary<string, Type[]> {
               ["SarcasticDante"] = new [] { typeof(bool), typeof(int), typeof(string) },
               ["N0tan3rd"] = new [] { typeof(string), typeof(bool), typeof(int) }
            },
         }
      };

      private static readonly ConcurrentDictionary<string, SortedDictionary<string, Type[]>[]> kHardTestData2 = new ConcurrentDictionary<string, SortedDictionary<string, Type[]>[]> {
         ["asdf"] = new[] {
            new SortedDictionary<string, Type[]> {
               ["Fred"] = new [] { typeof(int), typeof(string), typeof(bool) },
               ["Banana"] = new [] { typeof(int), typeof(bool), typeof(string) }
            },
         },
         ["jkl;"] = new[] {
            new SortedDictionary<string, Type[]> {
               ["saef"] = new [] { typeof(bool), typeof(int), typeof(string) },
               ["qwe"] = new [] { typeof(string), typeof(bool), typeof(int) }
            },
         }
      };

      private static readonly ConcurrentDictionary<Enum1, SortedDictionary<string, List<Enum2>>[]> kHardTestData3 = new ConcurrentDictionary<Enum1, SortedDictionary<string, List<Enum2>>[]> {
         [Enum1.A] = new[] {
            new SortedDictionary<string, List<Enum2>> {
               ["Fred"] = new List<Enum2> { Enum2.A, Enum2.B, Enum2.C },
               ["Banana"] = new List<Enum2> { Enum2.A, Enum2.A, Enum2.A }
            },
         },
         [Enum1.B] = new[] {
            new SortedDictionary<string, List<Enum2>> {
               ["saef"] = new List<Enum2> { Enum2.C, Enum2.C, Enum2.C },
               ["qwe"] = new List<Enum2> { Enum2.B, Enum2.B, Enum2.B }
            },
         }
      };

      private static readonly Type[] kTestTypes = { };

      public DictionaryLikeRoundtripFT() : base(kTestTypes) { }

      [Fact]
      public void EasyTest() {
         MultiThreadedRoundTripTest(new[] { kEasyTestData }, 1000, 8);
      }

      [Fact]
      public void HardTest1() {
         MultiThreadedRoundTripTest(new[] { kHardTestData1 }, 1000, 8);
      }

      [Fact]
      public void HardTest2() {
         MultiThreadedRoundTripTest(new[] { kHardTestData2 }, 1000, 8);
      }

      [Fact]
      public void HardTest3() {
         MultiThreadedRoundTripTest(new[] { kHardTestData3 }, 1000, 8);
      }

      public enum Enum1 { A, B, C }
      public enum Enum2 { A = int.MinValue, B = 0, C = int.MaxValue }
   }
}
