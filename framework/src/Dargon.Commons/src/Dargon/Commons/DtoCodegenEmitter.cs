using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Dargon.Commons {
   public class DtoCodegenEmitter : ICodegenEmitter {
      public static string EmitAsString(object x) {
         var sb = new StringBuilder();
         var tw = new StringBuilderTokenWriter { StringBuilder = sb };
         var emitter = new DtoCodegenEmitter { TokenWriter = tw };
         emitter.Visit(x);
         return sb.ToString();
      }

      public ITokenWriter TokenWriter { get; set; }

      public void Visit(object x) {
         if (x == null) {
            Emit("null");
            return;
         }

         if (x is ICodegenEmitable cd) {
            cd.EmitCodegen(this);
            return;
         }

         var t = x.GetType();
         if (t == typeof(char)) {
            Emit("'" + ((char)(object)x).ToStringLiteralChar() + "'");
            return;
         }

         if (t == typeof(string)) {
            Emit(((string)(object)x).ToEscapedStringLiteral());
            return;
         }

         if (t.IsValueType && t.IsPrimitive) {
            Emit(x.ToString());
            return;
         }
            
         if (x is IEnumerable enumerable) {
            if (t.IsArray) {
               Emit("new");
               EmitTypeName(t.GetElementType());
               Emit("[");
               Emit(((Array)x).Length.ToString());
               Emit("]");
            } else {
               EmitDefaultConstruction(t);
            }

            Emit("{");

            var enumerator = enumerable.GetEnumerator();
            for (var it = 0; enumerator.MoveNext(); it++) {
               if (it != 0) Emit(",");
               Visit(enumerator.Current);
            }

            Emit("}");
            return;
         }

         // assume simple struct
         var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
         var properties = t.GetProperties(bindingFlags);
         var fields = t.GetFields(bindingFlags);

         var supported = properties.All(p => p.IsAutoProperty_Slow()) &&
                         fields.All(f => f.IsPublic);

         if (!supported) {
            throw Assert.Fail("Unsupported for codegen serialization: " + t.ToString());
         }


         EmitDefaultConstruction(t);
         Emit("{");

         var setterCallbacks = new SortedDictionary<string, Action>();

         foreach (var field in fields) {
            setterCallbacks.Add(field.Name, () => this.EmitInitializerSet(field.Name, field.GetValue(x)));
         }
         foreach (var prop in properties) {
            setterCallbacks.Add(prop.Name, () => this.EmitInitializerSet(prop.Name, prop.GetValue(x)));
         }

         foreach (var (_, cb) in setterCallbacks) {
            cb();
         }
         Emit("}");
      }

      public void EmitConstructionStart(Type t) {
         Emit("new");
         EmitTypeName(t);
         Emit("(");
      }

      public void EmitConstructionEnd() {
         Emit(")");
      }

      public void EmitDefaultConstruction(Type t) {
         EmitConstructionStart(t);
         EmitConstructionEnd();
      }

      public void EmitTypeName(Type t) {
         var gargs = t.GenericTypeArguments;

         var name = t.Name;
         if (gargs.Length > 0) name = name.Substring(0, name.IndexOf("`", StringComparison.Ordinal).AssertIsGreaterThanOrEqualTo(0));
         Emit(name);

         if (gargs.Length > 0) {
            Emit("<");
            for (var i = 0; i < gargs.Length; i++) {
               if (i != 0) Emit(",");
               EmitTypeName(gargs[i]);
            }
            Emit(">");
         }
      }

      public void Emit(string token) => TokenWriter.Write(token);
   }

   public interface ICodegenEmitter {
      void Visit(object x);
      void EmitConstructionStart(Type t);
      void EmitConstructionEnd();
      void EmitDefaultConstruction(Type t);
      void EmitTypeName(Type t);
      void Emit(string token);
   }

   public static class CodegenEmitterHelpers {
      public static void EmitInitializerSet(this ICodegenEmitter emitter, string propertyName, object value) {
         emitter.Emit(propertyName);
         emitter.Emit("=");
         emitter.Visit(value);
         emitter.Emit(",");
      }
   }

   public interface ICodegenEmitable {
      void EmitCodegen(ICodegenEmitter emitter);
   }

   public interface ITokenWriter {
      void Write(string token);
   }

   public class StringBuilderTokenWriter : ITokenWriter {
      public StringBuilder StringBuilder;

      public void Write(string token) {
         StringBuilder.Append(token);
         StringBuilder.Append(' ');
      }
   }

   public static class DtoCodegenEmitterStatics {
      public static string ToCodegenDump(this object x) {
         return DtoCodegenEmitter.EmitAsString(x);
      }
   }
}
