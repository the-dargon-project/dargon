﻿using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Dargon.Commons.Collections;
using Dargon.Vox.Utilities;

namespace Dargon.Vox {
   public static class InternalRepresentationToHintTypeConverter {
      private delegate IEnumerable CastFunc(IEnumerable x);

      private static IGenericFlyweightFactory<CastFunc> castFuncs
         = GenericFlyweightFactory.ForMethod<CastFunc>(
            typeof(Enumerable),
            nameof(Enumerable.Cast));

      private static Array ShallowCloneArrayToType(Array elements, Type destinationArrayElementType) {
         var result = Array.CreateInstance(destinationArrayElementType, elements.Length);
         for (var i = 0; i < elements.Length; i++) {
            result.SetValue(
               ConvertCollectionToHintType(elements.GetValue(i), destinationArrayElementType),
               i);
         }
         return result;
      }

      private static IEnumerable ShallowCloneDictionaryToType(IEnumerable dictionary, Type destinationKvpType) {
         Trace.Assert(destinationKvpType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.KeyValuePair<,>));

         var genericArgs = destinationKvpType.GetGenericArguments();
         var keyType = genericArgs[0];
         var valueType = genericArgs[1];

         var listType = typeof(System.Collections.Generic.List<>).MakeGenericType(destinationKvpType);
         var result = Activator.CreateInstance(listType);
         var add = listType.GetMethods().First(m => m.Name == "Add" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == destinationKvpType);
         var enumerator = dictionary.GetEnumerator();
         while (enumerator.MoveNext()) {
            dynamic kvp = enumerator.Current;
            var key = ConvertCollectionToHintType(kvp.Key, keyType);
            var value = ConvertCollectionToHintType(kvp.Value, valueType);
            var castedIntermediateKvp = Activator.CreateInstance(destinationKvpType, key, value);
            add.Invoke(result, new object [] { castedIntermediateKvp });
         }
         return (IEnumerable)result;
      }

      public static object ConvertCollectionToHintType(object collection, Type hintType) {
         // fast fail on non-collections, since we aren't converting them.
         if (!typeof(IEnumerable).IsAssignableFrom(hintType)) {
            return collection;
         }

         // TODO: This will fail on nulls right now
         if (hintType.IsInstanceOfType(collection)) {
            return collection;
         }

         // If hintType is IROD/IROS, up that to the Dictionary default, since we can't 
         // construct an interface.
         // TODO: All sets are IROS, all dicts are IROD, so is this an impossible code path?
         if (hintType.IsGenericType && hintType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IReadOnlyDictionary<,>)) {
            hintType = typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(hintType.GetGenericArguments());
         }
         if (hintType.IsGenericType && hintType.GetGenericTypeDefinition() == typeof(IReadOnlySet<>)) {
            hintType = typeof(HashSet<>).MakeGenericType(hintType.GetGenericArguments());
         }

         var enumerableType = EnumerableUtilities.GetGenericIEnumerableInterfaceTypeOrNull(hintType);
         var elementType = EnumerableUtilities.GetEnumerableElementType(enumerableType);

         if (EnumerableUtilities.IsDictionaryLikeType(hintType)) {
            // e.g. KeyValuePair<TKey, TValue>[]
            var subjectKvpVectorLike = (IEnumerable)collection;
            var shallowClonedCastedElements = ShallowCloneDictionaryToType(subjectKvpVectorLike, elementType);

            // invoke .ctor(IEnumerable<KVP<TKey, TVal>>)
            var constructor = hintType.GetConstructor(new[] { enumerableType });
            if (constructor != null) {
               return constructor.Invoke(new object[] { shallowClonedCastedElements });
            }

            // invoke .ctor(), then Add/Enqueue of collection type
            var instance = Activator.CreateInstance(hintType);
            var add = hintType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                              .FirstOrDefault(m => m.Name.Contains("Add") && m.GetParameters().Length == 2);
            
            foreach (var kvpObject in shallowClonedCastedElements) {
               dynamic kvp = kvpObject;
               add.Invoke(instance, new object[] { kvp.Key, kvp.Value });
            }
            return instance;
         } else {
            // Vector-like case
            var subjectElements = (Array)collection;

            if (hintType.IsArray) {
               return ShallowCloneArrayToType(subjectElements, hintType.GetElementType());
            } else {
               // Invoke .ctor(IEnumerable<TElement>)
               var constructor = hintType.GetConstructor(new[] { enumerableType });
               if (constructor != null) {
                  var shallowClonedCastedElements = ShallowCloneArrayToType(subjectElements, elementType);
                  return constructor.Invoke(new object[] { shallowClonedCastedElements });
               }

               // Invoke .ctor(), then Add/Enqueue(TElement)
               var instance = Activator.CreateInstance(hintType);
               var add = hintType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                 .FirstOrDefault(m => m.Name.Contains("Add") && m.GetParameters().Length == 1) ?? hintType.GetMethod("Enqueue");
               foreach (var element in subjectElements) {
                  add.Invoke(instance, new object[] { element });
               }
               return instance;
            }
         }
      }
   }
}