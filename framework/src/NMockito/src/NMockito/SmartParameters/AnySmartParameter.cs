using System;
using System.Reflection;

namespace NMockito.SmartParameters {
   public class AnySmartParameter : SmartParameter {
      private readonly Type typeConstraint;

      public AnySmartParameter(Type typeConstraint) {
         this.typeConstraint = typeConstraint;
      }

      public bool Matches(object value) {
         return typeConstraint?.GetTypeInfo().IsInstanceOfType(value) ?? (value != null);
      }
   }
}