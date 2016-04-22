using System;
using System.Linq;
using System.Threading;

namespace Dargon.Courier.Utilities {
   public class BloomFilter {
      private readonly int bitsPerFilter;
      private readonly int hashCount;
      private readonly ConcurrentBitSet bitSet;

      public BloomFilter(int expectedCount, double falsePositiveProbability) {
         bitsPerFilter = (int)Math.Ceiling((expectedCount * Math.Log(falsePositiveProbability)) / Math.Log(1.0 / (Math.Pow(2.0, Math.Log(2.0)))));
         hashCount = (int)Math.Round((Math.Log(2.0) * bitsPerFilter) / expectedCount);
         bitSet = new ConcurrentBitSet((uint)bitsPerFilter);
      }

      public bool Test(Guid guid) {
         var hash1 = guid.GetHashCode();
         var hash2 = Hash(guid);
         var bits = Enumerable.Range(1, hashCount)
                              .Select(i => (uint)DoubleHashToBitNumber(hash1, hash2, i))
                              .Distinct();
         return bits.All(bitSet.Contains);
      }

      public bool SetAndTest(Guid guid) {
         var hash1 = guid.GetHashCode();
         var hash2 = Hash(guid);
         var bits = Enumerable.Range(1, hashCount)
                              .Select(i => (uint)DoubleHashToBitNumber(hash1, hash2, i))
                              .Distinct();
         bool result = false;
         foreach (var bit in bits) {
            result |= bitSet.TrySet(bit);
         }
         return result;
      }

      public void Clear() => bitSet.Clear();

      private int DoubleHashToBitNumber(int hash1, int hash2, int i) {
         return Math.Abs(hash1 + hash2 * i) % bitsPerFilter;
      }

      public static unsafe int Hash(Guid guid) {
         var pGuid = (ulong*)&guid;
         return Hash(pGuid[0]).GetHashCode() ^ Hash(pGuid[1]).GetHashCode();
      }

      public static ulong Hash(ulong input) {
         const ulong prime = 1099511628211;
         const ulong offset = 14695981039346656037;
         return offset + prime * input;
      }

      public class ConcurrentBitSet {
         private readonly uint size;
         private int[] storage;

         public ConcurrentBitSet(uint size) {
            this.size = size;
            this.storage = new int[(size + 31) / 32];
         }

         /// <summary>
         /// Returns a value indicating whether the specified bit
         /// in the bitset is set.
         /// </summary>
         /// <param name="bit">
         /// The zero-based index of the bit being checked.
         /// </param>
         /// <returns>
         /// Whether the bit of index <paramref name="bit"/> is set.
         /// </returns>
         public bool Contains(uint bit) {
            var index = bit / 32;
            var offset = bit % 32;
            var mask = 1 << (int)offset;
            int value = storage[index];
            return (value & mask) != 0;
         }

         /// <summary>
         /// Sets the n-th bit of the bitset. If the n-th bit is already set,
         /// then nothing happens.
         /// </summary>
         /// <param name="n">Zero-index of the bit to set.</param>
         /// <returns>
         /// Whether the operation mutated the bitset.
         /// </returns>
         /// <exception cref="ArgumentOutOfRangeException"></exception>
         public bool TrySet(uint n) {
            var index = n / 32;
            var offset = n % 32;
            var mask = 1 << (int)offset;
            int lastValue = 0;
            int readValue;
            int nextValue;
            do {
               readValue = lastValue;
               if ((readValue & mask) != 0) {
                  return false;
               }
               nextValue = readValue | mask;
            } while ((lastValue = Interlocked.CompareExchange(ref storage[index], nextValue, readValue)) != readValue);
            return true;
         }

         public void Clear() {
            for (var i = 0; i < storage.Length; i++) {
               storage[i] = 0;
            }
         }
      }
   }
}
