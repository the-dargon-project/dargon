using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMockito;
using Xunit;

namespace Dargon.Commons {
   public class CollectionStaticsTests : NMockitoInstance {
      [Fact]
      public void DictionaryMap_HappyPath_1() {
         for (var i = 0; i < 10; i++) {
            var dict = CreatePlaceholder<Dictionary<string, int>>();
            var dict2 = dict.Map(v => v * 10);
            foreach (var kvp in dict) {
               (kvp.Value * 10).AssertEquals(dict2[kvp.Key]);
            }
         }
      }

      [Fact]
      public void DictionaryMap_HappyPath_2() {
         for (var i = 0; i < 10; i++) {
            var dict = CreatePlaceholder<Dictionary<string, int>>();
            var dict2 = dict.Map((k, v) => $"{k}{v}");
            foreach (var kvp in dict) {
               $"{kvp.Key}{kvp.Value}".AssertEquals(dict2[kvp.Key]);
            }
         }
      }
   }
}
