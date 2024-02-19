using NMockito;
using System;
using Xunit;
using static Dargon.Commons.Collections.AlignedBitset;

namespace Dargon.Commons.Collections {
   public class BitsetTests : NMockitoInstance {
      [Fact]
      public void BitListTest() {
         for (var sz = 1; sz <= 256; sz++) {
            var set = new BitList(sz);
            var truth = new bool[set.Count];

            var r = new Random(sz);
            for (var it = 0; it < 256; it++) {
               for (var j = 0; j < 32; j++) {
                  var idx = r.Next(set.Count);
                  var value = r.Next(2) == 1;
                  truth[idx] = value;
                  set[idx] = value;
               }

               for (var i = 0; i < truth.Length; i++) {
                  truth[i].AssertEquals(set[i]);
               }
            }
         }
      }

      [Fact]
      public void AlignedBitsetTest() {
         for (var bitsPerItem = 1; bitsPerItem <= 64; bitsPerItem *= 2) {
            for (var sz = 1; sz <= 256; sz++) {
               var set = new AlignedBitset(sz, bitsPerItem);
               var truth = new nuint[set.Count];

               var r = new Random(bitsPerItem * 1337 + sz * 173);
               var maxValueExclusive = 1 << bitsPerItem;
               for (var it = 0; it < 256; it++) {
                  for (var j = 0; j < 32; j++) {
                     var idx = r.Next(set.Count);
                     var value = (nuint)r.Next(maxValueExclusive);
                     truth[idx] = value;
                     set[idx] = value;
                  }

                  for (var i = 0; i < truth.Length; i++) {
                     truth[i].AssertEquals(set[i]);
                  }
               }
            }
         }
      }

      [Fact]
      public void AlignmentConfigTests() {
         nuint.Size.AssertEquals(8); // 8 bytes, 64 bits

         var case1Bit = new AlignmentConfig(1);
         case1Bit.bitsPerItem.AssertEquals(1);
         case1Bit.indexToStoreOffsetShift.AssertEquals(6);
         case1Bit.indexToElementSubIndexMask.AssertEquals(0b11_1111);
         case1Bit.indexToElementSubIndexShift.AssertEquals(0);
         case1Bit.Compute(0).AssertEquals((0, 0, (nuint)1));
         case1Bit.Compute(1).AssertEquals((0, 1, (nuint)1));
         case1Bit.Compute(2).AssertEquals((0, 2, (nuint)1));
         case1Bit.Compute(63).AssertEquals((0, 63, (nuint)1));
         case1Bit.Compute(64).AssertEquals((1, 0, (nuint)1));
         case1Bit.Compute(64 * 100).AssertEquals((100, 0, (nuint)1));

         var case2Bit = new AlignmentConfig(2);
         case2Bit.bitsPerItem.AssertEquals(2);
         case2Bit.indexToStoreOffsetShift.AssertEquals(5);
         case2Bit.indexToElementSubIndexMask.AssertEquals(0b1_1111);
         case2Bit.indexToElementSubIndexShift.AssertEquals(1);
         case2Bit.Compute(0).AssertEquals((0, 0, (nuint)0b11));
         case2Bit.Compute(1).AssertEquals((0, 2, (nuint)0b11));
         case2Bit.Compute(31).AssertEquals((0, 62, (nuint)0b11));
         case2Bit.Compute(32).AssertEquals((1, 0, (nuint)0b11));
         case2Bit.Compute(32 * 100).AssertEquals((100, 0, (nuint)0b11));

         var case32Bit = new AlignmentConfig(nuint.Size * 8 / 2);
         case32Bit.bitsPerItem.AssertEquals(nuint.Size * 8 / 2);
         case32Bit.indexToStoreOffsetShift.AssertEquals(1);
         case32Bit.indexToElementSubIndexMask.AssertEquals(0b1);
         case32Bit.indexToElementSubIndexShift.AssertEquals(5);
         case32Bit.Compute(0).AssertEquals((0, 0, (nuint)0xFFFFFFFFu));
         case32Bit.Compute(1).AssertEquals((0, 32, (nuint)0xFFFFFFFFu));
         case32Bit.Compute(2).AssertEquals((1, 0, (nuint)0xFFFFFFFFu));
         case32Bit.Compute(3).AssertEquals((1, 32, (nuint)0xFFFFFFFFu));
         case32Bit.Compute(4).AssertEquals((2, 0, (nuint)0xFFFFFFFFu));
         case32Bit.Compute(2 * 100).AssertEquals((100, 0, (nuint)0xFFFFFFFFu));

         var caseMax = new AlignmentConfig(nuint.Size * 8);
         caseMax.bitsPerItem.AssertEquals(nuint.Size * 8);
         caseMax.indexToStoreOffsetShift.AssertEquals(0);
         caseMax.indexToElementSubIndexMask.AssertEquals(0);
         caseMax.indexToElementSubIndexShift.AssertEquals(6); // unused
         caseMax.Compute(0).AssertEquals((0, 0, nuint.MaxValue));
         caseMax.Compute(1).AssertEquals((1, 0, nuint.MaxValue));
         caseMax.Compute(2).AssertEquals((2, 0, nuint.MaxValue));
         caseMax.Compute(3).AssertEquals((3, 0, nuint.MaxValue));
         caseMax.Compute(4).AssertEquals((4, 0, nuint.MaxValue));
         caseMax.Compute(1 * 100).AssertEquals((100, 0, nuint.MaxValue));
      }
   }
}
