using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NMockito
{
   public static class NMockitoSmartParameters
   {
      private static readonly List<INMockitoSmartParameter> smartParameters = new List<INMockitoSmartParameter>();

      public static T Any<T>(Func<T, bool> test = null)
      {
         smartParameters.Add(new NMockitoAny<T>(test));
         return default(T);
      }

      public static T Eq<T>(T value)
      {
         smartParameters.Add(new NMockitoEquals(value));
         return default(T);
      }

      public static TCollection EqSequence<TCollection>(TCollection value)
         where TCollection : IEnumerable {
         smartParameters.Add(new NMockitoEqualsSequence(value));
         return default(TCollection);
      }

      internal static void __AddSmartParameter(INMockitoSmartParameter value) { smartParameters.Add(value); }

      internal static INMockitoSmartParameter[] CopyAndClearSmartParameters()
      {
         var result = smartParameters.ToArray();
         smartParameters.Clear();
         return result;
      }
   }
}