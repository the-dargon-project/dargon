using System;
using System.Collections.Generic;
using System.Linq;

namespace NMockito
{
   public class NMockitoEqualsSequence<T> : INMockitoSmartParameter
   {
      private readonly IEnumerable<T> value;
      public NMockitoEqualsSequence(IEnumerable<T> value) { this.value = value; }
      public bool Test(object value) { return this.value.SequenceEqual((IEnumerable<T>)value); }
      public override string ToString() { return "[EqualsSequence " + string.Join(", ", value) + "]"; }
      public override bool Equals(object obj) { return obj is NMockitoEqualsSequence<T> && ((NMockitoEqualsSequence<T>)obj).value.SequenceEqual(this.value); }
   }
}