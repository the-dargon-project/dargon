using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Dargon.Commons.Utilities {
   public static class GenericFlyweightFactory {
      public static IGenericFlyweightFactory<TMethod> ForStaticMethod<TMethod>(Type staticType, string methodName) where TMethod : Delegate {
         if (staticType.IsGenericTypeDefinition) {
            return ForStaticMethod_GivenTypeDefinition<TMethod>(staticType, methodName);
         } else {
            return ForStaticMethod_GivenMethodDefinition<TMethod>(staticType, methodName);
         }
      }

      private static IGenericFlyweightFactory<TMethod> ForStaticMethod_GivenTypeDefinition<TMethod>(Type staticTypeDefinition, string methodName) where TMethod : Delegate {
         return new GenericFlyweightFactoryImpl<TMethod>(
            t => {
               var staticType = staticTypeDefinition.MakeGenericType(t);
               var method = staticType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
               if (method == null) {
                  throw new KeyNotFoundException($"Could not find method of name {methodName} in {staticType.FullName}");
               }
               return ReflectionUtils.CreateAutocastingMethodInvoker<TMethod>(null, staticType, method);
            });
      }

      private static IGenericFlyweightFactory<TMethod> ForStaticMethod_GivenMethodDefinition<TMethod>(Type staticType, string methodName) where TMethod : Delegate {
         var methodDefinition = staticType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
         if (methodDefinition == null) {
            throw new KeyNotFoundException($"Could not find method definition of name {methodName} in {staticType.FullName}");
         }
         return new GenericFlyweightFactoryImpl<TMethod>(
            t => {
               var method = methodDefinition.MakeGenericMethod(t);
               return ReflectionUtils.CreateAutocastingMethodInvoker<TMethod>(null, staticType, method);
            });
      }

      public static IGenericFlyweightFactory<TMethod> ForInstanceMethod<TMethod>(object inst, string methodName) where TMethod : Delegate {
         var instType = inst.GetType();
         var methodDef = instType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).AssertIsNotNull();
         methodDef.IsGenericMethodDefinition.AssertIsTrue();
         return new GenericFlyweightFactoryImpl<TMethod>(
            t => {
               var method = methodDef.MakeGenericMethod(t);
               return ReflectionUtils.CreateAutocastingMethodInvoker<TMethod>(inst, instType, method);
            });
      }
   }
}