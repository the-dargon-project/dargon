using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Vox.RoundTripTests;
using Xunit;

namespace Dargon.Vox.FunctionalTests.RoundTripTests {
   public class VectorLikeRoundtripFT : RoundTripTest {
      private static readonly Type[] kEasyTestData = { typeof(string), typeof(int), typeof(object) };
      private static readonly List<Type[]>[] kHardTestData1 = {
         new List<Type[]> {
            new Type[] { typeof(string), typeof(int), typeof(object) },
            new Type[] { typeof(string[]), typeof(int[]), typeof(object[]) }
         }
      };

      private static readonly List<List<Type>>[] kHardTestData2 = {
         new List<List<Type>> {
            new List<Type> { typeof(string), typeof(int), typeof(object) },
            new List<Type> { typeof(string[]), typeof(int[]), typeof(object[]) }
         }
      };

      private static readonly Type[] kTestTypes = {
      };

      public VectorLikeRoundtripFT() : base(kTestTypes) { }

      [Fact]
      public void ArrayEasyTest() {
         MultiThreadedRoundTripTest(new[] { kEasyTestData }, 10000, 8);
      }

      [Fact]
      public void ArrayHardTest1() {
         MultiThreadedRoundTripTest(new[] { kHardTestData1 }, 10000, 8);
      }

      [Fact]
      public void ArrayHardTest2() {
         MultiThreadedRoundTripTest(new[] { kHardTestData2 }, 10000, 8);
      }

      [Fact]
      public void ListEasyTest() {
         MultiThreadedRoundTripTest(new object[] { new List<Type>(kEasyTestData) }, 10000, 8);
      }

      [Fact]
      public void ListHardTest1() {
         MultiThreadedRoundTripTest(new object[] { new List<List<Type[]>>(kHardTestData1) }, 10000, 8);
      }

      [Fact]
      public void ListHardTest2() {
         MultiThreadedRoundTripTest(new object[] { new List<List<List<Type>>>(kHardTestData2) }, 10000, 8);
      }
   }
}
