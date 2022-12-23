using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Dargon.Commons {
   public static partial class ReflectionUtils {
      public static int GetStructSize<T>() where T : unmanaged => Unsafe.SizeOf<T>();

      /// <summary>
      /// Creates a delegate from a given MethodInfo, with automatic type-casting.
      /// For example, given a method void(TClass), we can create a delegate void(Object),
      /// for a generated method which loads the stack with Object, then invokes the TClass-taking method.
      /// </summary>
      public static TDelegate CreateAutocastingMethodInvoker<TDelegate>(object instanceOpt, Type type, MethodInfo methodInfo) where TDelegate : Delegate {
         var delegateInvokeMethod = typeof(TDelegate).GetMethod(nameof(Func<object>.Invoke)).AssertIsNotNull();
         var delegateParameterTypes = delegateInvokeMethod.GetParameters().Map(p => p.ParameterType);

         var result = new DynamicMethod($"AutocastInvoker_{methodInfo.Name}", delegateInvokeMethod.ReturnType, delegateParameterTypes, type, true);
         var emitter = result.GetILGenerator();
         if (instanceOpt == null) {
            if (delegateParameterTypes.Length >= 1) emitter.Emit(OpCodes.Ldarg_0);
            if (delegateParameterTypes.Length >= 2) emitter.Emit(OpCodes.Ldarg_1);
            if (delegateParameterTypes.Length >= 3) emitter.Emit(OpCodes.Ldarg_2);
            if (delegateParameterTypes.Length >= 4) emitter.Emit(OpCodes.Ldarg_3);
            if (delegateParameterTypes.Length >= 5) throw new NotImplementedException();
         } else {
            emitter.Emit(OpCodes.Ldarg_0);
            if (delegateParameterTypes.Length >= 1) emitter.Emit(OpCodes.Ldarg_1);
            if (delegateParameterTypes.Length >= 2) emitter.Emit(OpCodes.Ldarg_2);
            if (delegateParameterTypes.Length >= 3) emitter.Emit(OpCodes.Ldarg_3);
            if (delegateParameterTypes.Length >= 4) throw new NotImplementedException();
         }

         emitter.Emit(OpCodes.Call, methodInfo);
         if (methodInfo.ReturnType.IsValueType && !delegateInvokeMethod.ReturnType.IsValueType) {
            emitter.Emit(OpCodes.Box, methodInfo.ReturnType);
         } else if (!methodInfo.ReturnType.IsValueType && delegateInvokeMethod.ReturnType.IsValueType) {
            throw new NotImplementedException(); // haven't written a test case for this yet.
         }
         emitter.Emit(OpCodes.Ret);

         if (instanceOpt == null) {
            return result.CreateDelegate<TDelegate>();
         } else {
            return result.CreateDelegate<TDelegate>(instanceOpt);
         }
      }
   }
}