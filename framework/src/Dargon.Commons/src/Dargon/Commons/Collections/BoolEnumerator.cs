using System.Collections;
using System.Collections.Generic;

namespace Dargon.Commons.Collections;

public struct BoolEnumerator : IEnumerator<bool>, IEnumerable<bool> {
   private byte state;
   public bool MoveNext() => ++state is 1 or 2;
   public void Reset() => state = 0;
   public bool Current => state == 2;
   object IEnumerator.Current => Current;
   public void Dispose() { }

   public BoolEnumerator GetEnumerator() => this;
   IEnumerator<bool> IEnumerable<bool>.GetEnumerator() => this;
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}