namespace Dargon.Commons.Utilities {
   /// <summary>
   /// Equivalent of <seealso cref="System.Nullable{T}"/> for where T is of reference type.
   /// Intentionally not named Maybe{T} as the language lacks support for pattern matching
   /// that would mimic the traditional maybe/some/none types, or something like
   /// <code> if (someMaybe is {} x) {} </code>
   /// </summary>
   public readonly struct ReferenceBox<T> where T : class {
      public readonly bool HasValue;
      private readonly T Value;

      public ReferenceBox() { }

      public ReferenceBox(T value) {
         HasValue = true;
         Value = value;
      }

      public T Unbox() {
         HasValue.AssertIsTrue();
         return Value;
      }
   }
}
