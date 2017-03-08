using System.Collections.Concurrent;
using System.Collections.Generic;
using NMockito;
using Xunit;

namespace Dargon.Commons.Collections {
   public class ConcurrentDictionaryUsabilityTest : NMockitoInstance {
      private readonly ConcurrentDictionary<int, string> dict = new ConcurrentDictionary<int, string>(
         new Dictionary<int, string> {
            [0] = "zero",
            [1] = "one"
         }
      );

      [Fact]
      public void ClearLacksAmbiguity() {
         dict.Clear();
      }

      [Fact]
      public void ContainsKeyLacksAmbiguity() {
         AssertTrue(dict.ContainsKey(0));
      }

      [Fact]
      public void GetEnumeratorLacksAmbiguity() {
         AssertNotNull(dict.GetEnumerator());
      }

      [Fact]
      public void TryGetValueLacksAmbiguity() {
         string value;
         dict.TryGetValue(0, out value);
         AssertEquals("zero", value);
      }

      [Fact]
      public void KeysLacksAmbiguity() {
         AssertTrue(new HashSet<int> { 0, 1 }.SetEquals(dict.Keys));
      }

      [Fact]
      public void ValuesLacksAmbiguity() {
         AssertTrue(new HashSet<string> { "zero", "one" }.SetEquals(dict.Values));
      }

      [Fact]
      public void CountLacksAmbiguity() {
         AssertEquals(2, dict.Count);
      }

      [Fact]
      public void IsEmptyLacksAmbiguity() {
         AssertFalse(dict.IsEmpty);
      }

      [Fact]
      public void IndexerLacksAmbiguity() {
         AssertEquals("zero", dict[0]);
      }

      [Fact]
      public void ReferenceImplicitlyCastableToIReadOnlyDictionary() {
         IReadOnlyDictionary<int, string> x = dict;
         AssertEquals(x, dict);
      }
   }
}
