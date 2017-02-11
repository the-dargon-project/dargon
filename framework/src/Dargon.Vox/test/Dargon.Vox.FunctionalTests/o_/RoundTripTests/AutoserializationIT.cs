using Dargon.Vox.InternalTestUtils;
using NMockito;
using System;
using System.Collections.Generic;
using System.Linq;
using Dargon.Commons;
using Xunit;

namespace Dargon.Vox.RoundTripTests {
   public class AutoserializationIT : NMockitoInstance {
      public AutoserializationIT() {
         TestTypeRegistrationHelpers.Register(typeof(LinkedListNode<>));
      }

      [Fact]
      public void Hodgepodge() {
         TestTypeRegistrationHelpers.RegisterWithSerializer<HodgepodgeDto>();
         RoundTripTest.Run(new HodgepodgeDto {
            True = true,
            False = false,
            Int8 = CreatePlaceholder<sbyte>(),
            Int16 = CreatePlaceholder<short>(),
            Int32 = CreatePlaceholder<int>(),
            String = CreatePlaceholder<string>(),
            Guid = CreatePlaceholder<Guid>(),
            IntList = CreatePlaceholder<List<int>>(),
            IntPowersArray = Enumerable.Range(-31, 31).Select(i => Math.Sign(i) * (1 << Math.Abs(i))).Concat(new [] { int.MinValue, int.MaxValue }).ToArray(),
            StringArray = CreatePlaceholder<string[]>(),
            IntStringMap = CreatePlaceholder<Dictionary<int, string>>(),
            IntStringStringArrayMapArrayMap = CreatePlaceholder<Dictionary<int, Dictionary<string, string[]>[]>>(),
            Type = typeof(LinkedListNode<int>),
            DateTime = DateTime.FromFileTime(2131891),
            Float = CreatePlaceholder<float>(),
            Double = CreatePlaceholder<double>(),
         });
      }

      [Fact]
      public void LinkedList_String_SingleNode() {
         LinkedList_RunTest(new LinkedListNode<string>(CreatePlaceholder<string>()));
      }

      [Fact]
      public void LinkedList_String_Chain() {
         LinkedList_RunTest(
            new LinkedListNode<string>(
               CreatePlaceholder<string>(),
               new LinkedListNode<string>(
                  CreatePlaceholder<string>(),
                  new LinkedListNode<string>(
                     CreatePlaceholder<string>()))));
      }

      private void LinkedList_RunTest<T>(LinkedListNode<T> node) {
         RoundTripTest.Run(node);
      }

      internal class HodgepodgeDto {
         public bool True { get; set; }
         public bool False { get; set; }
         public sbyte Int8 { get; set; }
         public short Int16 { get; set; }
         public int Int32 { get; set; }
         public string String { get; set; }
         public Guid Guid { get; set; }
         public List<int> IntList { get; set; }
         public string[] StringArray { get; set; }
         public int[] IntPowersArray { get; set; }
         public Dictionary<int, string> IntStringMap { get; set; }
         public Dictionary<int, Dictionary<string, string[]>[]> IntStringStringArrayMapArrayMap { get; set; }
         public Type Type { get; set; }
         public DateTime DateTime { get; set; }
         public double Float { get; set; }
         public double Double { get; set; }

         public override bool Equals(object obj) => Equals(obj as HodgepodgeDto);

         public bool Equals(HodgepodgeDto o) => o != null &&
                                                True == o.True &&
                                                False == o.False &&
                                                Int8 == o.Int8 &&
                                                Int16 == o.Int16 &&
                                                Int32 == o.Int32 &&
                                                Equals(String, o.String) &&
                                                Guid.Equals(o.Guid) &&
                                                IntList.SequenceEqual(o.IntList) &&
                                                StringArray.SequenceEqual(o.StringArray) &&
                                                IntPowersArray.SequenceEqual(o.IntPowersArray) &&
                                                IntStringMap.Count == o.IntStringMap.Count &&
                                                IntStringMap.All(kvp => o.IntStringMap[kvp.Key] == kvp.Value) &&
                                                IntStringStringArrayMapArrayMap.Count == o.IntStringStringArrayMapArrayMap.Count &&
                                                IntStringStringArrayMapArrayMap.All(kvp => {
                                                   var aDicts = kvp.Value;
                                                   var bDicts = o.IntStringStringArrayMapArrayMap[kvp.Key];
                                                   return aDicts.Length == bDicts.Length &&
                                                          aDicts.Zip(bDicts, Tuple.Create).All(
                                                             (dicts) => {
                                                                var aDict = dicts.Item1;
                                                                var bDict = dicts.Item2;
                                                                return aDict.Count == bDict.Count &&
                                                                       aDict.All(innerKvp => bDict[innerKvp.Key].SequenceEqual(innerKvp.Value));
                                                             });
                                                }) &&
                                                Type == o.Type &&
                                                DateTime == o.DateTime &&
                                                // ReSharper disable once CompareOfFloatsByEqualityOperator
                                                Float == o.Float &&
                                                // ReSharper disable once CompareOfFloatsByEqualityOperator
                                                Double == o.Double;
      }

      [AutoSerializable]
      internal class LinkedListNode<T> {
         public T Value { get; set; }
         public LinkedListNode<T> Next { get; set; }

         public LinkedListNode() {}

         public LinkedListNode(T value, LinkedListNode<T> next = null) {
            Value = value;
            Next = next;
         }

         public override bool Equals(object obj) {
            var other = obj as LinkedListNode<T>;
            return other != null &&
                   Value.Equals(other.Value) &&
                   Equals(Next, other.Next);
         }
      }
   }
}
