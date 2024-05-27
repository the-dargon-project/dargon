using Dargon.Commons;
using System;
using System.Linq;
using System.Reflection;
using Dargon.Ryu.Attributes;

namespace Dargon.Ryu.Internals {
   public static class TypeExtensions {
      public static Type[] GetRyuConstructorParameterTypes(this Type type) {
         return GetRyuConstructorOrThrow(type).GetParameters().Map(t => t.ParameterType);
      }

      public static ConstructorInfo GetRyuConstructorOrThrow(this Type type) {
         var constructors = type.GetTypeInfo().GetConstructors();
         switch (constructors.Length) {
            case 0:
               throw new NoConstructorsFoundException(type);
            case 1:
               return constructors.First();
            default:
               var ryuConstructors = constructors.Where(FilterRyuConstructor).ToList();
               if (ryuConstructors.Count != 1) {
                  throw new MultipleConstructorsFoundException(type);
               } else {
                  return ryuConstructors.First();
               }
         }
      }

      private static bool FilterRyuConstructor(ConstructorInfo constructor) {
         return constructor.HasAttribute<RyuConstructorAttribute>() &&
                !constructor.HasAttribute<RyuIgnoreConstructorAttribute>();
      }
   }
}
