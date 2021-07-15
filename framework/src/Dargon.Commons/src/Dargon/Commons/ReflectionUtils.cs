using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Dargon.Commons {
   public static partial class ReflectionUtils {
      public static int GetStructSize<T>() where T : unmanaged => Unsafe.SizeOf<T>();

      public static T CreateDelegateFromStaticMethodInfo<T>(Type staticType, MethodInfo methodInfo) {
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
      }
   }
}