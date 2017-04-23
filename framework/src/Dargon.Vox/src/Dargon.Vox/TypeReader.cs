using System;
using System.Collections.Generic;
using System.Linq;
using Dargon.Commons;
using Dargon.Commons.Collections;
using Dargon.Commons.Comparers;
using Dargon.Commons.Exceptions;

namespace Dargon.Vox {
   public class TypeReader {
      private readonly CopyOnAddDictionary<TypeIdsAndCount, Type> typesByTypeIdParts = new CopyOnAddDictionary<TypeIdsAndCount, Type>(new TypeIdsAndCountEqualityComparator());
      private readonly TypeRegistry typeRegistry;
      private readonly Func<TypeIdsAndCount, TypeIdsAndCount> cloneKeyFunc;
      private readonly Func<TypeIdsAndCount, Type> unpackTypesFunc;
      [ThreadStatic] private static int[] typeIdBuffer;

      public TypeReader(TypeRegistry typeRegistry) {
         this.typeRegistry = typeRegistry;
         this.cloneKeyFunc = CloneKey;
         this.unpackTypesFunc = input => UnpackTypes(typeRegistry, input);
      }

      private int[] GetTypeIdBuffer(int size) {
         if (typeIdBuffer == null || typeIdBuffer.Length < size) {
            typeIdBuffer = new int[size];
         }
         return typeIdBuffer;
      }

      public Type ReadType(VoxBinaryReader reader) {
         var typeCount = reader.ReadVariableInt();
         var typeIds = GetTypeIdBuffer(typeCount);
         for (var i = 0; i < typeCount; i++) {
            typeIds[i] = reader.ReadVariableInt();
         }
         var key = new TypeIdsAndCount { Count = typeCount, TypeIds = typeIds };
         var result = typesByTypeIdParts.GetOrAdd(key, cloneKeyFunc, unpackTypesFunc);
         return result;
      }

      private static TypeIdsAndCount CloneKey(TypeIdsAndCount arg) {
         return new TypeIdsAndCount { Count = arg.Count, TypeIds = arg.TypeIds.Take(arg.Count).ToArray() };
      }

      private static Type UnpackTypes(TypeRegistry typeRegistry, TypeIdsAndCount input) {
         var types = input.TypeIds.Take(input.Count).Select(typeRegistry.GetTypeOrThrow).ToArray();

         // special case: if types has 1 element and it's a generic type definition, return it
         if (types.Length == 1 && types[0].IsGenericTypeDefinition) {
            return types[0];
         }

         var s = new Stack<Type>();
         foreach (var type in types) {
            if (type == typeof(Array)) {
               s.Push(s.Pop().MakeArrayType());
            } else if (type.IsGenericTypeDefinition) {
               var genericArgs = Arrays.Create(type.GetGenericArguments().Length, s.Pop);
               s.Push(type.MakeGenericType(genericArgs));
            } else {
               s.Push(type);
            }
         }
         if (s.Count != 1) {
            throw new InvalidStateException($"Expected s.Count == 1 but found {s.Count}.");
         }
         return s.Pop();
      }
   }

   public struct TypeIdsAndCount {
      public int[] TypeIds;
      public int Count;
   }

   public class TypeIdsAndCountEqualityComparator : IEqualityComparer<TypeIdsAndCount> {
      public bool Equals(TypeIdsAndCount x, TypeIdsAndCount y) {
         if (x.Count != y.Count) {
            return false;
         }
         for (var i = 0; i < x.TypeIds.Length && i < y.TypeIds.Length; i++) {
            if (x.TypeIds[i] != y.TypeIds[i]) {
               return false;
            }
         }
         return true;
      }

      public int GetHashCode(TypeIdsAndCount obj) {
         var h = 13 + 17 * obj.Count;
         for (var i = 0; i < obj.Count && i < obj.TypeIds.Length; i++) {
            h = h * 17 + obj.TypeIds[i];
         }
         return h;
      }
   }
}