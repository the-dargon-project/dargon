using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

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

      public static bool HasAttribute<TAttribute>(this Enum enumValue)
         where TAttribute : Attribute {
         return enumValue.GetAttributeOrNull<TAttribute>() != null;
      }

      public static bool HasAttribute<TAttribute>(this object instance)
         where TAttribute : Attribute {
         return instance.GetAttributeOrNull<TAttribute>() != null;
      }

      public static bool HasAttribute<TAttribute>(this Type type)
         where TAttribute : Attribute {
         return type.GetAttributeOrNull<TAttribute>() != null;
      }

      public static bool HasAttribute<TAttribute>(this TypeInfo typeInfo)
         where TAttribute : Attribute {
         return typeInfo.GetAttributeOrNull<TAttribute>() != null;
      }

      /// <summary>
      /// Gets the attribute of Enum value
      /// </summary>
      /// <typeparam name="TAttribute"></typeparam>
      /// <param name="enumValue"></param>
      /// <returns></returns>
      public static TAttribute GetAttributeOrNull<TAttribute>(this Enum enumValue)
         where TAttribute : Attribute {
         var enumType = enumValue.GetType();
         var memberInfo = enumType.GetTypeInfo().DeclaredMembers.First(member => member.Name.Equals(enumValue.ToString()));
         var attributes = memberInfo.GetCustomAttributes(typeof(TAttribute), false);
         return (TAttribute)attributes.FirstOrDefault();
      }

      public static TAttribute GetAttributeOrNull<TAttribute>(this object instance)
         where TAttribute : Attribute {
         var instanceType = instance as Type ?? instance.GetType();
         return GetAttributeOrNull<TAttribute>(instanceType);
      }

      public static TAttribute GetAttributeOrNull<TAttribute>(this Type type)
         where TAttribute : Attribute {
         var typeInfo = type.GetTypeInfo();
         return GetAttributeOrNull<TAttribute>(typeInfo);
      }


      public static TAttribute GetAttributeOrNull<TAttribute>(this TypeInfo typeInfo)
         where TAttribute : Attribute {
         var attributes = typeInfo.GetCustomAttributes(typeof(TAttribute), false);
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
   }
}
