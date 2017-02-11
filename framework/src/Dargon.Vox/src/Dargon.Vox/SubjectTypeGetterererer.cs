using System;
using Dargon.Vox.Internals.TypePlaceholders;

namespace Dargon.Vox {
   public static class SubjectTypeGetterererer {
      public static Type GetSubjectType(object subject) {
         if (subject == null) {
            return typeof(TNull);
         } else if (subject is bool) {
            return (bool)subject ? typeof(TBoolTrue) : typeof(TBoolFalse);
         } else {
            var type = subject.GetType();
            if (typeof(Type).IsAssignableFrom(type)) {
               return typeof(Type);
            } else {
               return type;
            }
         }
      }
   }
}