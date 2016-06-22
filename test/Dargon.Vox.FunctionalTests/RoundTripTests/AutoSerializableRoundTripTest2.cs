﻿using Dargon.Vox.RoundTripTests;
using System;
using System.Collections.Generic;
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
         AssertEquals(reference.String, actual.String);
         AssertEquals(reference.Guid, actual.Guid);
         AssertEquals(reference.IntList, actual.IntList);
         AssertEquals(reference.IntPowersArray, actual.IntPowersArray);
         AssertEquals(reference.StringArray, actual.StringArray);
         AssertEquals(reference.IntStringMap, actual.IntStringMap);
         AssertEquals(reference.IntStringStringArrayMapArrayMap, actual.IntStringStringArrayMapArrayMap);
         AssertEquals(reference.Type, actual.Type);
         AssertEquals(reference.DateTime, actual.DateTime);
         AssertEquals(reference.Float, actual.Float);
         AssertEquals(reference.Double, actual.Double);
         AssertHodgepodgeEquals(reference.InnerHodgepodge, actual.InnerHodgepodge);
      }

      private HodgepodgeDto CreateHodgepodge(HodgepodgeDto innerHodgepodge) {
         return new HodgepodgeDto {
            True = true,
            False = false,
            Int8 = CreatePlaceholder<sbyte>(),
            Int16 = CreatePlaceholder<short>(),
            Int32 = CreatePlaceholder<int>(),
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
            InnerHodgepodge = innerHodgepodge
         };
      }

      [AutoSerializable]
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
         public float Float { get; set; }
         public double Double { get; set; }
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
         MultiThreadedRoundTripTest(cases, 10000, 8);
      }

      [AutoSerializable]
      internal class LinkedListNode<T> {
         public T Value { get; set; }
         public LinkedListNode<T> Next { get; set; }

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
      }
   }
}
