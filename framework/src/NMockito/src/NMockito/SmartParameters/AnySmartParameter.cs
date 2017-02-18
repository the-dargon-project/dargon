using System;

namespace NMockito.SmartParameters {
   public class AnySmartParameter : SmartParameter {
      private readonly Type typeConstraint;

      public AnySmartParameter(Type typeConstraint) {
         this.typeConstraint = typeConstraint;
      }

      public bool Matches(object value) {
         return typeConstraint?.IsInstanceOfType(value) ?? (value != null);
      }
   }
}