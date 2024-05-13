﻿using System;
using System.Collections.Generic;
using Dargon.Commons.Collections;
using Dargon.Commons.Templating;

namespace Dargon.Commons.Utilities {
   public static partial class BinarySearchExtensions {
      public static int BinarySearchPredicate<T, TPredicate>(this ExposedArrayList<T> items, TPredicate predicate)
         where TPredicate : struct, ITemplatePredicate<T> {
         return BinarySearchPredicate<T, TPredicate>(items, 0, items.Count - 1, predicate);
      }

      public static int BinarySearchPredicate<T, TPredicate>(this ExposedArrayList<T> items, int lo, int hi, TPredicate predicate)
         where TPredicate : struct, ITemplatePredicate<T> {
         while (lo < hi) {
            var mid = lo + (hi - lo) / 2;

            if (predicate.Invoke(items[mid])) {
               hi = mid;
            } else {
               lo = mid + 1;
            }
         }
         return !predicate.Invoke(items[lo]) ? -1 : lo;
      }

      public static int BinarySearchPredicate<T>(this ExposedArrayList<T> items, Func<T, bool> predicate) {
         return BinarySearchPredicate(items, 0, items.Count - 1, predicate);
      }

      public static int BinarySearchPredicate<T>(this ExposedArrayList<T> items, int lo, int hi, Func<T, bool> predicate) {
         while (lo < hi) {
            var mid = lo + (hi - lo) / 2;

            if (predicate(items[mid])) {
               hi = mid;
            } else {
               lo = mid + 1;
            }
         }
         return !predicate(items[lo]) ? -1 : lo;
      }
   }
}
