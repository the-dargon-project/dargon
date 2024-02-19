using System;
using Dargon.Commons.Templating;

namespace Dargon.Commons.Collections;

public class BitList {
   private readonly int bitCount;
   private ulong[] store;

   public BitList(int bitCount) {
      bitCount.AssertIsGreaterThanOrEqualTo(0);

      this.bitCount = bitCount;

      var elementCount = (int)Math.Ceiling(bitCount / 64.0f);
      store = new ulong[elementCount];
   }

   public int Count => bitCount;

   public bool this[int index] {
      get {
         ValidateIndex(index);

         var bit = 1ul << (index & 0b11_1111);
         var el = store[index >> 6];
         return (el & bit) != 0;
      }
      set {
         var bit = 1ul << (index & 0b11_1111);
         ref var el = ref store[index >> 6];
         el = (el & ~bit) | (value ? bit : 0);
      }
   }

   private void ValidateIndex(int index) {
#if !DEBUG
      return;
#endif
      index.AssertIsGreaterThanOrEqualTo(0);
      index.AssertIsLessThan(bitCount);
   }
}

public class AlignedBitset {
   private readonly int itemCount;
   private readonly AlignmentConfig alignmentConfig;
   private nuint[] store;

   public AlignedBitset(int itemCount, int bitsPerItem) {
      itemCount.AssertIsGreaterThanOrEqualTo(0);
      
      this.itemCount = itemCount;
      this.alignmentConfig = new(bitsPerItem);

      var elementCount = (int)Math.Ceiling(itemCount * bitsPerItem / (float)alignmentConfig.bitsPerNuint);
      store = new nuint[elementCount];
   }

   public int Count => itemCount;

   public nuint this[int index] {
      get {
         var x = alignmentConfig.Compute(index);
         var el = store[x.storeIndex];
         return (el >> x.packedRightShift) & x.mask;
      }
      set {
         var x = alignmentConfig.Compute(index);
         ref var el = ref store[x.storeIndex];
         el = (el & ~(x.mask << x.packedRightShift)) |
              (value & x.mask) << x.packedRightShift;
      }
   }

   public struct AlignmentConfig {
      public AlignmentConfig(int bitsPerItem) {
         var bitsPerNuint = nuint.Size * 8; // ex: 64-bit nuint 0b_0100_0000 (1 at idx 6)
         
         bitsPerItem.AssertIsGreaterThanOrEqualTo(1); // ex: 2
         bitsPerItem.AssertEquals((int)BitMath.GetLSB((uint)bitsPerItem));
         bitsPerItem.AssertIsLessThanOrEqualTo(bitsPerNuint);

         var itemsPerNuint = bitsPerNuint / bitsPerItem;

         this.bitsPerItem = bitsPerItem;
         this.indexToStoreOffsetShift = BitMath.GetLSBIndex((uint)bitsPerNuint) - BitMath.GetLSBIndex((uint)bitsPerItem);
         this.indexToElementSubIndexMask = (1 << indexToStoreOffsetShift) - 1;
         this.indexToElementSubIndexShift = BitMath.GetLSBIndex((uint)(bitsPerNuint / itemsPerNuint));
         this.bitsPerNuint = bitsPerNuint;
      }

      public readonly int bitsPerItem;
      public readonly int indexToStoreOffsetShift;
      public readonly int indexToElementSubIndexMask;
      public readonly int indexToElementSubIndexShift;
      public readonly int bitsPerNuint;

      public (int storeIndex, int packedRightShift, nuint mask) Compute(int index) {
         var storeIndex = index >> indexToStoreOffsetShift;
         var packedRightShift = (index & indexToElementSubIndexMask) << indexToElementSubIndexShift;
         var mask = bitsPerItem == bitsPerNuint ? nuint.MaxValue : (((nuint)1 << bitsPerItem) - 1);
         return (storeIndex, packedRightShift, mask);
      }
   }
}


public class AlignedBitset<TEnum, TEnumToNuintCaster> 
   where TEnum : Enum
   where TEnumToNuintCaster : ITemplateBidirectionalCast<TEnum, nuint> {
   private readonly AlignedBitset inner;

   public AlignedBitset(int itemCount, int bitsPerItem) {
      inner = new(itemCount, bitsPerItem);
   }

   public int Count => inner.Count;

   public TEnum this[int index] {
      get => default(TEnumToNuintCaster).Cast(inner[index]);
      set => inner[index] = default(TEnumToNuintCaster).Cast(value);
   }
}
