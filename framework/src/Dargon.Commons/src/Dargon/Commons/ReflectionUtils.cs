using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons {
   public static class ReflectionUtils {
      public static void Zero<T>(T x) {
         // Todo: Support byref value types
         if (typeof(T).IsClass) ClassZeroUtils<T>.Invoke(x);
         else throw new InvalidOperationException();
      }

      public static void DefaultReconstruct<T>(T x) {
         ClassDefaultReconstructor<T>.Invoke(x);
      }

      private static class ClassZeroUtils<T> {
         public delegate void InvokerFunc(T x);

         private static InvokerFunc invoke;
         public static InvokerFunc Invoke => invoke ?? (invoke = Compile());

         private static InvokerFunc Compile() {
            var method = new DynamicMethod("", null, new[] { typeof(T) }, typeof(T), true);
            var emitter = method.GetILGenerator();

            var i = 0;
            foreach (var field in typeof(T).GetTypeInfo().DeclaredFields) {
               // if (i++ == 7) break;
               if (field.IsStatic) continue;
               //Console.WriteLine(field.Name);

               emitter.Emit(OpCodes.Ldarg_0);
               var fieldType = field.FieldType;
               if (!fieldType.IsPrimitive) {
                  if (fieldType.IsValueType) {
                     emitter.Emit(OpCodes.Ldflda, field);
                     emitter.Emit(OpCodes.Initobj, fieldType);
                  } else {
                     emitter.Emit(OpCodes.Ldnull);
                     emitter.Emit(OpCodes.Stfld, field);
                  }
               } else {
                  if (fieldType == typeof(float)) {
                     emitter.Emit(OpCodes.Ldc_R4, 0.0f);
                  } else if (fieldType == typeof(double)) {
                     emitter.Emit(OpCodes.Ldc_R8, 0.0);
                  } else if (fieldType == typeof(long) || fieldType == typeof(ulong)) {
                     emitter.Emit(OpCodes.Ldc_I4_0);
                     emitter.Emit(OpCodes.Conv_I8);
                  } else {
                     emitter.Emit(OpCodes.Ldc_I4_0);
                  }
                  emitter.Emit(OpCodes.Stfld, field);
               }
            }
            emitter.Emit(OpCodes.Ret);
            return (InvokerFunc)method.CreateDelegate(typeof(InvokerFunc));
         }
      }

      private static class ClassDefaultReconstructor<T> {
         public delegate void InvokerFunc(T x);

         private static InvokerFunc invoke;
         public static InvokerFunc Invoke => invoke ?? (invoke = Compile());

         private static InvokerFunc Compile() {
            var method = new DynamicMethod("", null, new[] { typeof(T) }, typeof(T), true);
            var emitter = method.GetILGenerator();
            emitter.Emit(OpCodes.Ldarg_0);
            emitter.Emit(OpCodes.Call, typeof(T).GetTypeInfo().DeclaredConstructors.First(c => c.GetParameters().All(p => p.HasDefaultValue)));
            emitter.Emit(OpCodes.Ret);
            return (InvokerFunc)method.CreateDelegate(typeof(InvokerFunc));
         }
      }
   }
}
