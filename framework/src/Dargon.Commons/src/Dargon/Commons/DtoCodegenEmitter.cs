using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Dargon.Commons.Collections;

namespace Dargon.Commons {
   public class DtoCodegenEmitter : ICodegenEmitter {
      private Dictionary<object, int> captures = new(ReferenceEqualityComparer.Instance);
      private Dictionary<int, bool> isEmitComplete = new();
      private List<Action> lateEmits = new List<Action>();
      private int visitDepth = 0;

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

         var t = x.GetType();
         if (t == typeof(char)) {
            Emit("'" + ((char)(object)x).ToStringLiteralChar() + "'");
            return;
         } else if (t == typeof(string)) {
            Emit(((string)(object)x).ToEscapedStringLiteral());
            return;
         } else if (t == typeof(float)) {
            Emit(((float)(object)x).ToString("R") + "f");
            return;
         } else if (t == typeof(double)) {
            Emit(((double)(object)x).ToString("R"));
            return;
         } else if (t == typeof(bool)) {
            Emit((bool)(object)x ? "true" : "false");
            return;
         } else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
            var keyProp = t.GetProperty("Key", BindingFlags.Public | BindingFlags.Instance);
            var key = keyProp.GetValue(x);

            var valueProp = t.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
            var value = valueProp.GetValue(x);
            Emit("new");
            EmitTypeName(t);
            Emit("(");
            Visit(key);
            Emit(",");
            Visit(value);
            Emit(")");
            return;
         } else if (t == typeof(Guid)) {
            Emit("new Guid(");
            Visit(((Guid)(object)x).ToByteArray());
            Emit(")");
            return;
         }

         if (t.IsValueType && t.IsPrimitive) {
            Emit(x.ToString());
            return;
         }

         int? captureIdOrNull = null;
         if (!x.GetType().IsValueType) {
            if (captures.TryGetValue(x, out var existingId)) {
               EmitTypeName(typeof(TlsDtoCodegenEmitDeserializationState));
               Emit(".");
               Emit(nameof(TlsDtoCodegenEmitDeserializationState.CodegenGet));
               Emit("<");
               EmitTypeName(x.GetType());
               Emit(">");
               Emit("(");
               Visit(existingId);
               Emit(")");
               return;
            }

            captureIdOrNull = captures.Count;
            captures.Add(x, captureIdOrNull.Value);
            isEmitComplete[captureIdOrNull.Value] = false;
         }

         if (x is ICodegenEmitable cd) {
            cd.EmitCodegen(this);
         } else if (x is IEnumerable enumerable) {
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
         } else {
            // assume simple struct
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var properties = t.GetProperties(bindingFlags);
            var fields = t.GetFields(bindingFlags);

            var supported = properties.All(p => p.IsAutoProperty_Slow()) &&
                            fields.Where(f => !(f.Name.Contains("BackingField") && f.HasAttribute<CompilerGeneratedAttribute>()))
                                  .All(f => f.IsPublic);

            if (!supported) {
               throw Assert.Fail("Unsupported for codegen serialization: " + t.ToString());
            }


            EmitDefaultConstruction(t);
            Emit("{");

            var setterCallbacks = new SortedDictionary<string, Action>();

            foreach (var field in fields) {
               setterCallbacks.Add(field.Name, () => this.EmitInitializerSet(x, field.Name, field.GetValue(x), ","));
            }

            foreach (var prop in properties) {
               setterCallbacks.Add(prop.Name, () => this.EmitInitializerSet(x, prop.Name, prop.GetValue(x), ","));
            }

            visitDepth++;
            foreach (var (_, cb) in setterCallbacks) {
               cb();
            }
            visitDepth--;

            Emit("}");
         }

         if (captureIdOrNull.HasValue) {
            Emit(".");
            Emit(nameof(TlsDtoCodegenEmitDeserializationState.CodegenStoreAtIndex));
            Emit("(");
            Visit(captureIdOrNull.Value);
            Emit(")");
          
            isEmitComplete[captureIdOrNull.Value] = true;
         }

         if (visitDepth == 0) {
            Emit(".");
            Emit(nameof(Instances.Tap));
            Emit("(");
            Emit("()");
            Emit("=>");
            Emit("{");
            foreach (var le in lateEmits) {
               le();
            }
            Emit("}");
            Emit(")");
         }
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

      public bool IsObservedButNotYetEmitted(object x, out int instanceId) {
         return captures.TryGetValue(x, out instanceId) && !isEmitComplete[instanceId];
      }

      public void AddLateEmit(Action a) {
         lateEmits.Add(a);
      }
   }

   public interface ICodegenEmitter {
      void Visit(object x);
      void EmitConstructionStart(Type t);
      void EmitConstructionEnd();
      void EmitDefaultConstruction(Type t);
      void EmitTypeName(Type t);
      void Emit(string token);
      bool IsObservedButNotYetEmitted(object x, out int instanceId);
      void AddLateEmit(Action a);
   }

   public static class CodegenEmitterHelpers {
      private static Dictionary<Type, bool> typeShouldLateEmitReferenceMembers = new();

      private static bool ShouldLateEmitReferenceMembers(Type t) =>
         typeShouldLateEmitReferenceMembers.TryGetValue(t, out var res)
            ? res
            : typeShouldLateEmitReferenceMembers[t] = t.HasAttribute<DtoCodegenEmitterRecursiveAttribute>();

      public static void EmitInitializerSet(this ICodegenEmitter emitter, object source, string propertyName, object value, string trailingTokenOpt = null, bool isLateEmitCall = false) {
         if (!isLateEmitCall && value is not null && (
               ShouldLateEmitReferenceMembers(value.GetType()) ||
               emitter.IsObservedButNotYetEmitted(value, out var instanceId)
             )) {
            emitter.AddLateEmit(() => {
               emitter.IsObservedButNotYetEmitted(source, out instanceId).AssertIsFalse();

               emitter.EmitTypeName(typeof(TlsDtoCodegenEmitDeserializationState));
               emitter.Emit(".");
               emitter.Emit(nameof(TlsDtoCodegenEmitDeserializationState.CodegenGet));
               emitter.Emit("<");
               emitter.EmitTypeName(source.GetType());
               emitter.Emit(">");
               emitter.Emit("(");
               emitter.Visit(instanceId);
               emitter.Emit(")");
               emitter.Emit(".");
               EmitInitializerSet(emitter, source, propertyName, value, null, true);
               emitter.Emit(";");
            });
         } else {
            emitter.Emit(propertyName);
            emitter.Emit("=");
            emitter.Visit(value);
            if (trailingTokenOpt != null) emitter.Emit(trailingTokenOpt);
         }
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

   public static class TlsDtoCodegenEmitDeserializationState {
      [ThreadStatic] private static ExposedArrayList<object> tlsObjectByIdStore;
      private static ExposedArrayList<object> TlsObjectById => tlsObjectByIdStore ??= new ExposedArrayList<object>(4); // must nonzero initial capacity

      public static T CodegenStoreAtIndex<T>(this T o, int index) {
         TlsObjectById.EnsureCapacity(index + 1);
         TlsObjectById.size = TlsObjectById.store.Length;
         TlsObjectById[index] = o;
         return o;
      }

      public static T CodegenGet<T>(int index) => (T)tlsObjectByIdStore[index].AssertIsNotNull();

      public static T CodegenClearAndPass<T>(this T t) {
         TlsObjectById.Clear();
         return t;
      }
   }

   public static class DtoCodegenEmitterStatics {
      public static string ToCodegenDump(this object x) {
         return DtoCodegenEmitter.EmitAsString(x);
      }
   }

   public class DtoCodegenEmitterRecursiveAttribute : Attribute { }
}
