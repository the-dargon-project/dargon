using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Dargon.Commons {
   public static class DictionaryMapperExtensions {
      public static Dictionary<K, V2> Map<K, V1, V2>(this Dictionary<K, V1> dict, Func<V1, V2> mapper) {
         return DictionaryMapperInternals<K, V1, V2>.dictionaryMapper(dict, (k, v) => mapper(v));
      }

      public static Dictionary<K, V2> Map<K, V1, V2>(this Dictionary<K, V1> dict, Func<K, V1, V2> mapper) {
         return DictionaryMapperInternals<K, V1, V2>.dictionaryMapper(dict, mapper);
      }

      public static Dictionary<T, V> UniqueMap<T, V>(this IReadOnlyList<T> list, Func<T, V> mapper) {
         var result = new Dictionary<T, V>();
         for (var i = 0 ; i < list.Count; i++) {
            var x = list[i];
            result[x] = mapper(x);
         }
         return result;
      }

      public static class DictionaryMapperInternals<K, V1, V2> {
         public delegate Dictionary<K, V2> DictionaryMapFunc(Dictionary<K, V1> dict, Func<K, V1, V2> mapper);

         public static readonly DictionaryMapFunc dictionaryMapper;

         static DictionaryMapperInternals() {
            FieldInfo FindField(Type t, string name) {
               var res = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
               if (res != null) return res;

               // legacy framework code doesn't have the _ prefix.
               // Sometime circa .net5 they updated this.
               return t.GetField("_" + name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }

            PropertyInfo FindProperty(Type t, string name) => t.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var tInputDict = typeof(Dictionary<K, V1>);
            var tInputEntry = tInputDict.GetNestedType("Entry", BindingFlags.NonPublic).MakeGenericType(typeof(K), typeof(V1));
            var tOutputDict = typeof(Dictionary<K, V2>);
            var tOutputEntry = tOutputDict.GetNestedType("Entry", BindingFlags.NonPublic).MakeGenericType(typeof(K), typeof(V2));
            var tMapper = typeof(Func<K, V1, V2>);

            // in the new .net dictionary, fields were preprended with _, e.g. _buckets.
            var isNewDotnetDictionary = tInputDict.GetField("buckets", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) == null;

            var method = new DynamicMethod("", tOutputDict, new[] { tInputDict, tMapper }, true);
            var emitter = method.GetILGenerator();
            var outputDictLocal = emitter.DeclareLocal(tOutputDict);
            var inputDictEntriesLocal = emitter.DeclareLocal(tInputEntry.MakeArrayType());
            var outputDictEntriesLocal = emitter.DeclareLocal(tOutputEntry.MakeArrayType());
            var iLocal = emitter.DeclareLocal(typeof(int));

            //-------------------------------------------------------------------------------------
            // alloc output dict, store to loc 0 (this allocs w/ capacity 0, null internal store)
            // the reason we don't call ctor with size overhead is that runs a zeroing init loop.
            //-------------------------------------------------------------------------------------
            var tOutputDictConstructor = tOutputDict.GetConstructors().First(c => c.GetParameters().Length == 0);
            emitter.Emit(OpCodes.Newobj, tOutputDictConstructor);
            emitter.Emit(OpCodes.Stloc, outputDictLocal);

            emitter.Emit(OpCodes.Ldarg_0);
            emitter.Emit(OpCodes.Call, FindProperty(tInputDict, "Count").GetMethod);
            emitter.Emit(OpCodes.Ldc_I4_0);
            emitter.Emit(OpCodes.Ceq);
            var exitLabel = emitter.DefineLabel();
            emitter.Emit(OpCodes.Brtrue, exitLabel);

            //-------------------------------------------------------------------------------------
            // clone buckets (int[]), store into output dict
            //-------------------------------------------------------------------------------------
            emitter.Emit(OpCodes.Ldloc, outputDictLocal); // push this onto stack, used later in store.

            // load buckets
            emitter.Emit(OpCodes.Ldarg_0);
            emitter.Emit(OpCodes.Ldfld, FindField(tInputDict, "buckets"));

            // clone
            var cloneIntArrayOrNull = typeof(DictionaryMapperInternals<K, V1, V2>).GetMethod(nameof(CloneIntArrayOrNull), BindingFlags.NonPublic | BindingFlags.Static);
            emitter.Emit(OpCodes.Call, cloneIntArrayOrNull);

            // store
            emitter.Emit(OpCodes.Stfld, FindField(tOutputDict, "buckets"));

            //-------------------------------------------------------------------------------------
            // alloc clone of Dictionary<TKey, TValue>.Entry[] entries, store into output dict
            //-------------------------------------------------------------------------------------
            // find input dict entries local
            emitter.Emit(OpCodes.Ldarg_0);
            emitter.Emit(OpCodes.Ldfld, FindField(tInputDict, "entries"));
            emitter.Emit(OpCodes.Stloc, inputDictEntriesLocal);

            // allocate output dict entries local
            emitter.Emit(OpCodes.Ldloc, outputDictLocal); // push this onto stack, used later in store.
            emitter.Emit(OpCodes.Ldloc, inputDictEntriesLocal);
            emitter.Emit(OpCodes.Ldlen);
            emitter.Emit(OpCodes.Conv_I4);
            emitter.Emit(OpCodes.Newarr, tOutputEntry);
            emitter.Emit(OpCodes.Stloc, outputDictEntriesLocal);
            emitter.Emit(OpCodes.Ldloc, outputDictEntriesLocal);
            emitter.Emit(OpCodes.Stfld, FindField(tOutputDict, "entries"));

            // init i = 0
            var forLoopConditional = emitter.DefineLabel();
            var forLoopBody = emitter.DefineLabel();
            var forLoopIncrementor = emitter.DefineLabel();
            emitter.Emit(OpCodes.Ldc_I4_0);
            emitter.Emit(OpCodes.Stloc, iLocal);

            // jump to conditional of for loop (the bounds check)
            emitter.Emit(OpCodes.Br, forLoopConditional);

            // for loop body
            emitter.MarkLabel(forLoopBody);

            // Load TOutDict.Entry* 4x on stack
            emitter.Emit(OpCodes.Ldloc, outputDictEntriesLocal);
            emitter.Emit(OpCodes.Ldloc, iLocal);
            emitter.Emit(OpCodes.Ldelema, tOutputEntry);
            emitter.Emit(OpCodes.Dup);
            emitter.Emit(OpCodes.Dup);
            emitter.Emit(OpCodes.Dup);

            // copy hashcode
            emitter.Emit(OpCodes.Ldloc, inputDictEntriesLocal);
            emitter.Emit(OpCodes.Ldloc, iLocal);
            emitter.Emit(OpCodes.Ldelema, tInputEntry);
            emitter.Emit(OpCodes.Ldfld, FindField(tInputEntry, "hashCode"));
            emitter.Emit(OpCodes.Stfld, FindField(tOutputEntry, "hashCode"));

            // copy next
            emitter.Emit(OpCodes.Ldloc, inputDictEntriesLocal);
            emitter.Emit(OpCodes.Ldloc, iLocal);
            emitter.Emit(OpCodes.Ldelema, tInputEntry);
            emitter.Emit(OpCodes.Ldfld, FindField(tInputEntry, "next"));
            emitter.Emit(OpCodes.Stfld, FindField(tOutputEntry, "next"));

            // copy key
            emitter.Emit(OpCodes.Ldloc, inputDictEntriesLocal);
            emitter.Emit(OpCodes.Ldloc, iLocal);
            emitter.Emit(OpCodes.Ldelema, tInputEntry);
            emitter.Emit(OpCodes.Ldfld, FindField(tInputEntry, "key"));
            emitter.Emit(OpCodes.Stfld, FindField(tOutputEntry, "key"));

            //-------------------------------------------------------------------------------------
            // map (key, value) => new value if i < dict.count
            //-------------------------------------------------------------------------------------
            var cleanupAndJumpToIncrementor = emitter.DefineLabel();

            // verify mapping conditions
            emitter.Emit(OpCodes.Ldloc, iLocal);
            emitter.Emit(OpCodes.Ldarg_0);
            emitter.Emit(OpCodes.Ldfld, FindField(tInputDict, "count"));
            emitter.Emit(OpCodes.Clt);
            emitter.Emit(OpCodes.Brfalse, cleanupAndJumpToIncrementor);

            // // dup TOutputDict.Entry*, then get ith element hashCode field
            // emitter.Emit(OpCodes.Dup);
            // emitter.Emit(OpCodes.Ldfld, FindField(tOutputEntry, "hashCode"));
            // emitter.Emit(OpCodes.Ldc_I4_0);
            // emitter.Emit(OpCodes.Clt);
            // emitter.Emit(OpCodes.Brtrue, cleanupAndJumpToIncrementor);

            // the actual mapping code
            emitter.Emit(OpCodes.Ldarg_1);

            emitter.Emit(OpCodes.Ldloc, inputDictEntriesLocal);
            emitter.Emit(OpCodes.Ldloc, iLocal);
            emitter.Emit(OpCodes.Ldelema, tInputEntry);
            emitter.Emit(OpCodes.Ldfld, FindField(tInputEntry, "key"));

            emitter.Emit(OpCodes.Ldloc, inputDictEntriesLocal);
            emitter.Emit(OpCodes.Ldloc, iLocal);
            emitter.Emit(OpCodes.Ldelema, tInputEntry);
            emitter.Emit(OpCodes.Ldfld, FindField(tInputEntry, "value"));

            var invokeMethod = tMapper.GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance);
            emitter.Emit(OpCodes.Callvirt, invokeMethod);
            emitter.Emit(OpCodes.Stfld, FindField(tOutputEntry, "value"));

            emitter.Emit(OpCodes.Br, forLoopIncrementor);

            // cleanup if decided to skip map value
            emitter.MarkLabel(cleanupAndJumpToIncrementor);
            emitter.Emit(OpCodes.Pop);

            // i++
            emitter.MarkLabel(forLoopIncrementor);
            emitter.Emit(OpCodes.Ldloc, iLocal);
            emitter.Emit(OpCodes.Ldc_I4_1);
            emitter.Emit(OpCodes.Add);
            emitter.Emit(OpCodes.Stloc, iLocal);

            // for loop conditional
            emitter.MarkLabel(forLoopConditional);
            emitter.Emit(OpCodes.Ldloc, iLocal);
            emitter.Emit(OpCodes.Ldloc, inputDictEntriesLocal);
            emitter.Emit(OpCodes.Ldlen);
            emitter.Emit(OpCodes.Conv_I4);
            emitter.Emit(OpCodes.Clt);
            emitter.Emit(OpCodes.Brtrue, forLoopBody);

            //-------------------------------------------------------------------------------------
            // clone count, version, freeList, freeCount
            //-------------------------------------------------------------------------------------
            var fieldNames = new[] {
               // existed since old .NET Framework
               (true, "count"),
               (true, "version"),
               (true, "freeList"),
               (true, "freeCount"),
               (true, "comparer"),

               // Added ~.NET 5
               (false, "fastModMultiplier"),
            };
            foreach (var (required, fieldName) in fieldNames) {
               var inputField = FindField(tInputDict, fieldName);
               var outputField = FindField(tOutputDict, fieldName);

               (inputField == null).AssertEquals(outputField == null);

               if (inputField == null) {
                  required.AssertIsFalse();
                  continue;
               }

               emitter.Emit(OpCodes.Ldloc, outputDictLocal);
               emitter.Emit(OpCodes.Ldarg_0);
               emitter.Emit(OpCodes.Ldfld, inputField);
               emitter.Emit(OpCodes.Stfld, outputField);
            }

            emitter.MarkLabel(exitLabel);
            emitter.Emit(OpCodes.Ldloc, outputDictLocal);
            emitter.Emit(OpCodes.Ret);

            dictionaryMapper = (DictionaryMapFunc)method.CreateDelegate(typeof(DictionaryMapFunc));
         }

         private static int[] CloneIntArrayOrNull(int[] input) {
            if (input == null) return null;
            var result = new int[input.Length];
            Buffer.BlockCopy(input, 0, result, 0, input.Length * sizeof(int));
            return result;
         }
      }
   }
}
 