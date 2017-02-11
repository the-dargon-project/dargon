using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Dargon.Commons;

namespace Dargon.Vox.Utilities {
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
               return CreateDelegateFromStaticMethodInfo<TMethod>(staticType, method);
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
               return CreateDelegateFromStaticMethodInfo<TMethod>(staticType, method);
            });
      }

      private static T CreateDelegateFromStaticMethodInfo<T>(Type staticType, MethodInfo methodInfo) {
         var invokeMethod = typeof(T).GetMethod(nameof(Func<object>.Invoke));
         var parameterTypes = invokeMethod.GetParameters().Map(p => p.ParameterType);

         var result = new DynamicMethod("", invokeMethod.ReturnType, parameterTypes, staticType, true);
         var emitter = result.GetILGenerator();
         if (parameterTypes.Length >= 1) emitter.Emit(OpCodes.Ldarg_0);
         if (parameterTypes.Length >= 2) emitter.Emit(OpCodes.Ldarg_1);
         if (parameterTypes.Length >= 3) emitter.Emit(OpCodes.Ldarg_2);
         if (parameterTypes.Length >= 4) emitter.Emit(OpCodes.Ldarg_3);
         if (parameterTypes.Length >= 5) throw new NotImplementedException();
         emitter.Emit(OpCodes.Call, methodInfo);
         if (methodInfo.ReturnType.IsValueType && !invokeMethod.ReturnType.IsValueType) {
            emitter.Emit(OpCodes.Box, methodInfo.ReturnType);
         }
         emitter.Emit(OpCodes.Ret);

         return (T)(object)result.CreateDelegate(typeof(T));
//         return (T)typeof(T).GetConstructor(new[] { typeof(T) }).Invoke(new object[] { result });
      }
   }
}