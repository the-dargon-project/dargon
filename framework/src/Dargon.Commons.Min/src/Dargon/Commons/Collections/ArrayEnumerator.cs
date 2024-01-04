using System.Collections;
using System.Collections.Generic;

namespace Dargon.Commons.Collections {
   public struct ArrayEnumerator<T> : IEnumerator<T> {
      private readonly T[] arr;
      private int currentIndex;

      public ArrayEnumerator(T[] arr) {
         this.arr = arr;
         this.currentIndex = -1;
      }

      public bool MoveNext() {
         if (currentIndex + 1 >= arr.Length) return false;
         currentIndex++;
         return true;
      }

      public void Reset() {
         currentIndex = -1;
      }

      public T Current => arr[currentIndex];
      object IEnumerator.Current => Current;

      public void Dispose() { }
   }
}