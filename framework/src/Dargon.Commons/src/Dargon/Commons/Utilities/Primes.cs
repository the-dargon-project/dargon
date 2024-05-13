using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Commons.Collections;
using Dargon.Commons.Templating;

namespace Dargon.Commons.Utilities {
   public static class Primes {
      public static readonly List<int> Store;

      static Primes() {
         Store = GeneratePrimes(1_000_000_000);
         Console.WriteLine(Store.ToCodegenDump());
      }

      static List<int> GeneratePrimes(int limit) {
         var isPrime = new BitList(limit);
         isPrime.Clear(true);
         isPrime[0] = false;
         isPrime[1] = false;

         for (var factor = 2; factor * factor < limit; factor++) {
            if (!isPrime[factor]) continue; // composite
            for (var composite = factor * factor; composite < limit; composite += factor) {
               isPrime[composite] = false;
            }
         }

         var res = new List<int>();
         for (var i = 0; i < limit; i++) {
            if (isPrime[i]) {
               res.Add(i);
            }
         }
         return res;
      }

      /// <summary>
      /// Finds first prime greater-than or equal to the given value
      /// </summary>
      public static int Ceil(int n) {
         var idx = Store.BinarySearchPredicate(new IsGreaterThanOrEqualTo(n));
         if (idx < 0) throw new NotSupportedException($"No prime greater than {n} contained in {nameof(Store)}.");
         return Store[idx];
      }
   }
}
