using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NMockito
{
   public class NMockitoEqualsSequence : INMockitoSmartParameter
   {
      private readonly IEnumerable value;
      public NMockitoEqualsSequence(IEnumerable value) { this.value = value; }
      public bool Test(object value) { return this.value.Cast<Object>().SequenceEqual(((IEnumerable)value).Cast<Object>()); }
      public override string ToString() { return "[EqualsSequence " + string.Join(", ", value) + "]"; }
      public override bool Equals(object obj) { return obj is NMockitoEqualsSequence && ((NMockitoEqualsSequence)obj).value.Cast<Object>().SequenceEqual(this.value.Cast<Object>()); }
   }
}