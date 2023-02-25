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

      public static bool HasAttribute<TAttribute>(this Type type, bool inherit = true)
         where TAttribute : Attribute {
         return type.GetAttributeOrNull<TAttribute>(inherit) != null;
      }

      public static bool HasAttribute<TAttribute>(this MemberInfo memberInfo, bool inherit = true)
         where TAttribute : Attribute {
         return memberInfo.GetCustomAttribute<TAttribute>(inherit) != null;
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

      public static TAttribute GetAttributeOrNull<TAttribute>(this MemberInfo memberInfo, bool inherit = true)
         where TAttribute : Attribute {
         return memberInfo.GetCustomAttribute<TAttribute>(inherit);
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

      public static T GetDefaultOfType<T>(this T self) => default(T);

      public static T Tap<T>(this T self, Action<T> func) {
         func(self);
         return self;
      }

      public static T[] TapEach<T>(this T[] self, Action<T> func) {
         foreach (var x in self) {
            func(x);
         }

         return self;
      }

      public static TEnumerable TapEach<T, TEnumerable>(this TEnumerable self, Action<T> func) where TEnumerable : IEnumerable<T> {
         foreach (var x in self) {
            func(x);
         }

         return self;
      }

      public static T Tee<T>(this T self, Action cb) {
         cb();
         return self;
      }

      public static T Tee<T>(this T self, Action<T> cb) {
         cb(self);
         return self;
      }

      public static U Then<T, U>(this T self, Func<T, U> func) {
         return func(self);
      }

      public static U Pipe<T, U>(this T self, Func<T, U> cb) => cb(self);

      public static T If<T>(this T self, bool cond) => self.IfElse(cond, default);

      public static T IfElse<T>(this T self, bool cond, T fallback) => cond ? self : fallback;

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
