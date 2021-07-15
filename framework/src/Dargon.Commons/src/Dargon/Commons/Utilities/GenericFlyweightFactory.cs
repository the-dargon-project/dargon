using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Dargon.Commons.Utilities {
   public static class GenericFlyweightFactory {
      public static IGenericFlyweightFactory<TMethod> ForMethod<TMethod>(Type staticType, string methodName) {
         if (staticType.IsGenericTypeDefinition) {
            return ForMethod_GivenTypeDefinition<TMethod>(staticType, methodName);
         } else {
            return ForMethod_GivenMethodDefinition<TMethod>(staticType, methodName);
         }
      }

      private static IGenericFlyweightFactory<TMethod> ForMethod_GivenTypeDefinition<TMethod>(Type staticTypeDefinition, string methodName) {
         return new GenericFlyweightFactoryImpl<TMethod>(
            t => {
               var staticType = staticTypeDefinition.MakeGenericType(t);
               var method = staticType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
               if (method == null) {
                  throw new KeyNotFoundException($"Could not find method of name {methodName} in {staticType.FullName}");
               }
               return ReflectionUtils.CreateDelegateFromStaticMethodInfo<TMethod>(staticType, method);
            });
      }

      private static IGenericFlyweightFactory<TMethod> ForMethod_GivenMethodDefinition<TMethod>(Type staticType, string methodName) {
         var methodDefinition = staticType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
         if (methodDefinition == null) {
            throw new KeyNotFoundException($"Could not find method definition of name {methodName} in {staticType.FullName}");
         }
         return new GenericFlyweightFactoryImpl<TMethod>(
            t => {
               var method = methodDefinition.MakeGenericMethod(t);
               return ReflectionUtils.CreateDelegateFromStaticMethodInfo<TMethod>(staticType, method);
            });
      }
   }
}