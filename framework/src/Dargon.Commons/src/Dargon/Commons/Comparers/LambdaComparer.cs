﻿//http://stackoverflow.com/questions/3130922/sortedsett-and-anonymous-icomparert-in-the-constructor-is-not-working

using System;
using System.Collections;
using System.Collections.Generic;

namespace Dargon.Commons.Comparers {
   [Obsolete("Consider using Comparer<T>.Create in .NET 4.5")]
   public class LambdaComparer<T> : IComparer<T> {
      private readonly Comparison<T> comparison;

      public LambdaComparer(Comparison<T> comparison) {
         this.comparison = comparison;
      }

      public int Compare(T x, T y) {
         return comparison(x, y);
      }
   }

   public class LambdaComparer : IComparer {
      private readonly Func<object, object, int> comparison;

      public LambdaComparer(Func<object, object, int> comparison) {
         this.comparison = comparison;
      }

      public int Compare(object x, object y) {
         return comparison(x, y);
      }
   }
}
