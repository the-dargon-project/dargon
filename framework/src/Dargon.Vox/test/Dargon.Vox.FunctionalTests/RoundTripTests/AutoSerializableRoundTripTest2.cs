using Dargon.Vox.RoundTripTests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Dargon.Vox.FunctionalTests.RoundTripTests {
   public class AutoSerializableRoundTripTest2 : RoundTripTest {
      private static readonly Type[] kTestTypes = { typeof(HodgepodgeDto), typeof(LinkedListNode<>) };
      public AutoSerializableRoundTripTest2() : base(kTestTypes) { }

      [Fact]
      public void Hodgepodge() {
         var hodgepodge = CreateHodgepodge(CreateHodgepodge(null));
         MultiThreadedRoundTripTest(
            new [] { hodgepodge }, 1000, 8,
            AssertHodgepodgeEquals);
      }

      private void AssertHodgepodgeEquals(HodgepodgeDto reference, HodgepodgeDto actual) {
         if (reference == null || actual == null) {
            AssertNull(reference);
            AssertNull(actual);
            return;
         }

         AssertEquals(reference.True, actual.True);
         AssertEquals(reference.False, actual.False);
         AssertEquals(reference.Int8, actual.Int8);
         AssertEquals(reference.Int16, actual.Int16);
         AssertEquals(reference.Int32, actual.Int32);
         AssertEquals(reference.Int64, actual.Int64);
         AssertEquals(reference.UInt8, actual.UInt8);
         AssertEquals(reference.UInt16, actual.UInt16);
         AssertEquals(reference.UInt32, actual.UInt32);
         AssertEquals(reference.UInt64, actual.UInt64);
         AssertEquals(reference.FileAccess, actual.FileAccess);
         AssertEquals(reference.String, actual.String);
         AssertEquals(reference.Guid, actual.Guid);
         AssertSequenceEquals(reference.IntList, actual.IntList);
         AssertSequenceEquals(reference.IntPowersArray, actual.IntPowersArray);
         AssertSequenceEquals(reference.StringArray, actual.StringArray);
         AssertSequenceEquals(reference.IntStringMap, actual.IntStringMap);
         AssertSequenceEquals(reference.IntStringStringArrayMapArrayMap, actual.IntStringStringArrayMapArrayMap);
         AssertEquals(reference.Type, actual.Type);
         AssertEquals(reference.DateTime, actual.DateTime);
         AssertEquals(reference.Float, actual.Float);
         AssertEquals(reference.Double, actual.Double);
         AssertEquals(reference.ValueTuple, actual.ValueTuple);
         AssertHodgepodgeEquals(reference.InnerHodgepodge, actual.InnerHodgepodge);
      }

      private HodgepodgeDto CreateHodgepodge(HodgepodgeDto innerHodgepodge) {
         return new HodgepodgeDto {
            True = true,
            False = false,
            Int8 = CreatePlaceholder<sbyte>(),
            Int16 = CreatePlaceholder<short>(),
            Int32 = CreatePlaceholder<int>(),
            Int64 = CreatePlaceholder<long>(),
            UInt8 = CreatePlaceholder<byte>(),
            UInt16 = CreatePlaceholder<ushort>(),
            UInt32 = CreatePlaceholder<uint>(),
            UInt64 = CreatePlaceholder<ulong>(),
            FileAccess = CreatePlaceholder<FileAccess>(),
            String = CreatePlaceholder<string>(),
            Guid = CreatePlaceholder<Guid>(),
            IntList = CreatePlaceholder<List<int>>(),
            IntPowersArray = Enumerable.Range(-31, 31).Select(i => Math.Sign(i) * (1 << Math.Abs(i))).Concat(new[] { int.MinValue, int.MaxValue }).ToArray(),
            StringArray = CreatePlaceholder<string[]>(),
            IntStringMap = CreatePlaceholder<Dictionary<int, string>>(),
            IntStringStringArrayMapArrayMap = new Dictionary<int, Dictionary<string, string[]>[]>(), // empty due to exponenetial runtime of createplaceholder
            Type = typeof(LinkedListNode<int>),
            DateTime = DateTime.FromFileTime(2131891),
            Float = CreatePlaceholder<float>(),
            Double = CreatePlaceholder<double>(),
            ValueTuple = CreatePlaceholder<(int, string)>(),
            InnerHodgepodge = innerHodgepodge,
         };
      }

      [AutoSerializable]
      internal class HodgepodgeDto {
         public bool True { get; set; }
         public bool False { get; set; }
         public sbyte Int8 { get; set; }
         public short Int16 { get; set; }
         public int Int32 { get; set; }
         public long Int64 { get; set; }
         public byte UInt8 { get; set; }
         public ushort UInt16 { get; set; }
         public uint UInt32 { get; set; }
         public ulong UInt64 { get; set; }
         public FileAccess FileAccess { get; set; }
         public string String { get; set; }
         public Guid Guid { get; set; }
         public List<int> IntList { get; set; }
         public string[] StringArray { get; set; }
         public int[] IntPowersArray { get; set; }
         public Dictionary<int, string> IntStringMap { get; set; }
         public Dictionary<int, Dictionary<string, string[]>[]> IntStringStringArrayMapArrayMap { get; set; }
         public Type Type { get; set; }
         public DateTime DateTime { get; set; }
         public float Float { get; set; }
         public double Double { get; set; }
         public ValueTuple<int, string> ValueTuple { get; set; }
         public HodgepodgeDto InnerHodgepodge { get; set; }
      }

      [Fact]
      public void LinkedList_String_Chain() {
         var cases = new[] {
            new LinkedListNode<string>(
               CreatePlaceholder<string>(),
               new LinkedListNode<string>(
                  CreatePlaceholder<string>(),
                  new LinkedListNode<string>(
                     CreatePlaceholder<string>())))
         };
         RunRoundTripTest(cases, "", 1);
         MultiThreadedRoundTripTest(cases, 10000, 8);
      }

      [AutoSerializable]
      internal class LinkedListNode<T> {
         public T Value { get; }
         public LinkedListNode<T> Next { get; }

         public LinkedListNode() { }

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

         public override int GetHashCode() => (Value == null ? 0 : Value.GetHashCode()) ^ (Next == null ? 0 : Next.GetHashCode());
      }
   }
}
