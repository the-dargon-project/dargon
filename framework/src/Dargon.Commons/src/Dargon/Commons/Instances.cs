using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Dargon.Commons.Exceptions;

namespace Dargon.Commons {
   /// <summary>
   /// Extension methods that apply to all objects.
   /// </summary>
   public static class Instances {
      /// <summary>                                                                                              
      /// Checks whether argument is <see langword="null"/> and throws <see cref="ArgumentNullException"/> if so.
      /// </summary>                                                                                             
      /// <param name="argument">Argument to check on <see langword="null"/>.</param>                            
      /// <param name="argumentName">Argument name to pass to Exception constructor.</param>                     
      /// <returns>Specified argument.</returns>                                                                 
      /// <exception cref="ArgumentNullException"/>
      [DebuggerStepThrough]
      public static T ThrowIfNull<T>(this T argument, string argumentName)
         where T : class {
         if (argument == null) {
            throw new ArgumentNullException(argumentName);
         } else {
            return argument;
         }
      }

      public static bool HasAttribute<TAttribute>(this Enum enumValue, bool inherit = true)
         where TAttribute : Attribute {
         return enumValue.GetAttributeOrNull<TAttribute>(inherit) != null;
      }

      public static bool HasAttribute<TAttribute>(this object instance, bool inherit = true)
         where TAttribute : Attribute {
         return instance.GetAttributeOrNull<TAttribute>(inherit) != null;
      }

      public static bool HasAttribute<TAttribute>(this Type type, bool inherit = true)
         where TAttribute : Attribute {
         return type.GetAttributeOrNull<TAttribute>(inherit) != null;
      }

      public static bool HasAttribute<TAttribute>(this TypeInfo typeInfo, bool inherit = true)
         where TAttribute : Attribute {
         return typeInfo.GetCustomAttribute<TAttribute>(inherit) != null;
      }

      public static bool HasAttribute<TAttribute>(this MethodInfo methodInfo, bool inherit = true)
         where TAttribute : Attribute {
         return methodInfo.GetCustomAttribute<TAttribute>(inherit) != null;
      }

      public static bool HasAttribute<TAttribute>(this FieldInfo fieldInfo, bool inherit = true)
         where TAttribute : Attribute {
         return fieldInfo.GetCustomAttribute<TAttribute>(inherit) != null;
      }

      public static bool HasAttribute<TAttribute>(this PropertyInfo propertyInfo, bool inherit = true)
         where TAttribute : Attribute {
         return propertyInfo.GetCustomAttribute<TAttribute>(inherit) != null;
      }

      /// <summary>
      /// Gets the attribute of Enum value
      /// </summary>
      /// <returns></returns>
      public static TAttribute GetAttributeOrNull<TAttribute>(this Enum enumValue, bool inherit = true)
         where TAttribute : Attribute {
         var enumType = enumValue.GetType();
         var memberInfo = enumType.GetTypeInfo().DeclaredMembers.First(member => member.Name.Equals(enumValue.ToString()));
         var attributes = memberInfo.GetCustomAttributes(typeof(TAttribute), inherit);
         return (TAttribute)attributes.FirstOrDefault();
      }

      public static TAttribute GetAttributeOrNull<TAttribute>(this object instance, bool inherit = true)
         where TAttribute : Attribute {
         var instanceType = instance as Type ?? instance.GetType();
         return GetAttributeOrNull<TAttribute>(instanceType, inherit);
      }

      public static TAttribute GetAttributeOrNull<TAttribute>(this Type type, bool inherit = true)
         where TAttribute : Attribute {
         var typeInfo = type.GetTypeInfo();
         return GetAttributeOrNull<TAttribute>(typeInfo, inherit);
      }


      public static TAttribute GetAttributeOrNull<TAttribute>(this TypeInfo typeInfo, bool inherit = true)
         where TAttribute : Attribute {
         var attributes = typeInfo.GetCustomAttributes(typeof(TAttribute), inherit);
         return (TAttribute)attributes.FirstOrDefault();
      }

      public static KeyValuePair<TKey, TValue> PairValue<TKey, TValue>(this TKey key, TValue value) {
         return new KeyValuePair<TKey, TValue>(key, value);
      }

      public static KeyValuePair<TKey, TValue> PairKey<TKey, TValue>(this TValue value, TKey key) {
         return key.PairValue(value);
      }

      public static IEnumerable<T> Wrap<T>(this T e) {
         yield return e;
      }

      public static void Noop(this object self) { }
      public static void Noop<T>(this T self) { }

      /// <summary>
      /// Returns what is conceptually equivalent to some hashcode of the object's underlying ID
      /// in runtime. Realistically I'm just providing this extension so I can avoid namespace
      /// importing System.Runtime.CompilerServices everywhere, since that brings in a whole
      /// bunch of scope pollution (e.g. DependencyAttribute).
      ///
      /// Like any hashcode, this value should not be presumed to be unique; it would be valid
      /// (though poor for performance) if this always returned 0, for example.
      /// </summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static int GetObjectIdHash<T>(this T o) {
         if (typeof(T).IsValueType) throw new GenericArgumentException<T>(); // inlined and elided.
         return RuntimeHelpers.GetHashCode(o);
      }
   }
}
