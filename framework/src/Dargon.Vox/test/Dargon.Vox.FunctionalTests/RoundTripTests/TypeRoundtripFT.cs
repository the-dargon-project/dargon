using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Dargon.Vox.Internals.TypePlaceholders;
using Xunit;

namespace Dargon.Vox.RoundTripTests {
   public class TypeRoundTripFT : RoundTripTest {
      public class GenericType1<T1> { }
      public class GenericType2<T1, T2> { }
      public class GenericType3<T1, T2, T3> { }

      private static readonly Type[][] kTypeArrayArray = {
         new Type[] { },
         new[] {
            typeof(int),
            typeof(string),
            typeof(Dictionary<,>),
            typeof(GenericType1<>),
            typeof(GenericType2<,>),
            typeof(GenericType3<,,>),
         },
         new[] {
            typeof(object),
            typeof(TypePlaceholderNull),
            typeof(void),
            typeof(int),
            typeof(string),
            typeof(Dictionary<int, string>),
            typeof(Dictionary<int, string[]>),
            typeof(Dictionary<int[], string[]>)
         },
         new[] {
            typeof(GenericType1<int>),
            typeof(GenericType2<int, string>),
            typeof(GenericType3<int, string, object>),
            typeof(GenericType1<GenericType2<GenericType1<int[]>, GenericType1<string[][][]>>>[]),
            typeof(GenericType3<int, string[], object[]>[])
         },
         new[] {
            typeof(Dictionary<GenericType1<int[]>, GenericType2<string, int[]>>),
            typeof(Dictionary<GenericType1<int[]>, GenericType2<string, int[]>>[]),
            typeof(GenericType1<GenericType2<GenericType1<int[]>, GenericType1<string[][][]>>>[]),
            typeof(GenericType3<int, string[], object[]>[])
         }
      };

      private static readonly Type[] kTestTypes = new [] {
         typeof(GenericType1<>),
         typeof(GenericType2<,>),
         typeof(GenericType3<,,>)
      };

      public TypeRoundTripFT() : base(kTestTypes) {

      }

      [Fact]
      public void TypeRoundTripTest() {
         var typesToSerialize = kTypeArrayArray.SelectMany(arr => arr).ToArray();
         MultiThreadedRoundTripTest(typesToSerialize, 1000, 8);
      }

      [Fact]
      public void TypeArrayRoundTripTest() {
         //         RunRoundTripTest(kTypeArrayArray[0], "", 2);
         MultiThreadedRoundTripTest(kTypeArrayArray, 1000, 8);
      }

      [Fact]
      public void TypeArrayArrayRoundTripTest() {
         MultiThreadedRoundTripTest(new[] { kTypeArrayArray }, 1000, 8);
      }

      [Fact]
      public void TypeSimplificationTest() {
         var tests = new Dictionary<Type, Type> {
            [typeof(GenericType1<SortedDictionary<int, string>>)] = typeof(GenericType1<Dictionary<int, string>>),
            [typeof(GenericType1<Dictionary<List<int>, ConcurrentDictionary<string, int[]>>>)] = typeof(GenericType1<Dictionary<int[], Dictionary<string, int[]>>>)
         };
         foreach (var test in tests) {
            AssertEquals(test.Value, serializer.Clone(test.Key));
         }
      }
   }
}
