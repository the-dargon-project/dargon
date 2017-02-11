using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dargon.Commons;
using Dargon.Commons.Collections;

namespace Dargon.Vox.Utilities {
   public static class TypeSimplifier {
      private static readonly ConcurrentDictionary<Type, Type> simplifiedTypeByType = new ConcurrentDictionary<Type, Type>();

      public static Type SimplifyType(Type type) {
         if (typeof(Type).IsAssignableFrom(type)) {
            return typeof(Type);
         } else if (type == typeof(string)) {
            return type;
         } else if (type.IsArray) {
            return simplifiedTypeByType.GetOrAdd(
               type,
               add => SimplifyType(type.GetElementType()).MakeArrayType());
         } else if (type.IsGenericType) {
            return simplifiedTypeByType.GetOrAdd(type, SimplifyGenericType);
         } else if (type.IsEnum) {
            return type.GetEnumUnderlyingType();
         } else {
            return type;
         }
      }

      private static Type SimplifyGenericType(Type genericType) {
         var enumerableInterfaceType = EnumerableUtilities.GetGenericIEnumerableInterfaceTypeOrNull(genericType);
         if (enumerableInterfaceType != null) {
            return SimplifyEnumerableType(enumerableInterfaceType);
         } else {
            return genericType.GetGenericTypeDefinition()
                              .MakeGenericType(
                                 genericType.GetGenericArguments().Map(SimplifyType)
               );
         }
      }

      private static Type SimplifyEnumerableType(Type enumerableInterfaceType) {
         var elementType = SimplifyType(enumerableInterfaceType.GetGenericArguments()[0]);
         if (elementType.IsGenericType &&
             elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
            var kvpGenericArgs = elementType.GetGenericArguments();
            return typeof(Dictionary<,>).MakeGenericType(kvpGenericArgs);
         }
         return elementType.MakeArrayType();
      }
   }

   public static class EnumerableUtilities {
      public static Type GetGenericIEnumerableInterfaceTypeOrNull(Type collectionType) {
         if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
            return collectionType;
         }

         return collectionType.GetInterfaces()
                              .FirstOrDefault(i => i.Name.Contains(nameof(IEnumerable)) && i.IsGenericType);
      }

      public static Type GetEnumerableElementType(Type type) {
         if (type.IsArray) {
            return type.GetElementType();
         } else {
            var enumerableType = GetGenericIEnumerableInterfaceTypeOrNull(type);
            return enumerableType.GetGenericArguments()[0];
         }
      }

      public static bool IsDictionaryLikeType(Type enumerableType) {
         return enumerableType.GetInterfaces()
                              .Any(t => t == typeof(IDictionary) ||
                                        (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ||
                                        (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
            );
      }
   }
}