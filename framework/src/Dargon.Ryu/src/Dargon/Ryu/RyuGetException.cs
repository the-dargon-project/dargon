using System;
using Dargon.Ryu.Attributes;

namespace Dargon.Ryu {
   public class RyuGetException : Exception {
      public RyuGetException(Type gettingType, Exception innerException)
         : base("While constructing " + gettingType.FullName, innerException) {
      }
   }

   public class RyuActivateException : Exception {
      public RyuActivateException(Type activatingType, Exception innerException)
         : base("While activating " + activatingType.FullName, innerException) { }
   }

   public class RyuInjectRequiredFieldsAttributeNotSpecifiedException : Exception {
      public RyuInjectRequiredFieldsAttributeNotSpecifiedException(Type activatingType)
         : base($"The type declares dependency attributes but lacks {nameof(InjectRequiredFields)}: " + activatingType.FullName) { }
   }
}
