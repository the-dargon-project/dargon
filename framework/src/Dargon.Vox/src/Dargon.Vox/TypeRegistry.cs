using System;
using System.Threading;
using Dargon.Commons.Collections;
using Dargon.Commons.Exceptions;
using Dargon.Vox.Utilities;

namespace Dargon.Vox {
   public class TypeRegistry {
      private readonly CopyOnAddDictionary<Type, TypeContextAndTypeId> typeContextAndTypeIdByType = new CopyOnAddDictionary<Type, TypeContextAndTypeId>();
      private readonly CopyOnAddDictionary<int, TypeContextAndTypeId> typeContextAndTypeIdByTypeId = new CopyOnAddDictionary<int, TypeContextAndTypeId>();

      public int GetTypeIdOrThrow(Type type) => typeContextAndTypeIdByType[type].TypeId;

      public void RegisterReservedTypeId(Type type, int typeId, Func<object> activator = null) {
         if (typeId > 0) {
            throw new ArgumentOutOfRangeException();
         }

         activator = activator ?? (() => {
            throw new InvalidOperationException($"Reserved Type did not specify activator.");
         });

         ImportType(new TypeContextAndTypeId(typeId, new TypeContext(type, activator)));
      }

      public Type GetTypeOrThrow(int typeId) {
         return typeContextAndTypeIdByTypeId[typeId].Type;
      }

      public void ImportTypes(VoxTypes voxTypes) {
         foreach (var kvp in voxTypes.EnumerateTypes()) {
            var typeId = kvp.Key;
            var typeContext = kvp.Value;
            var typeContextAndTypeId = new TypeContextAndTypeId(typeId, typeContext);
            ImportType(typeContextAndTypeId);
         }
      }

      private void ImportType(TypeContextAndTypeId typeContextAndTypeId) {
         var resultTypeContextAndTypeId = typeContextAndTypeIdByType.GetOrAdd(typeContextAndTypeId.Type, add => typeContextAndTypeId);
         if (typeContextAndTypeId.TypeId != resultTypeContextAndTypeId.TypeId) {
            throw new InvalidStateException($"Type {typeContextAndTypeId.Type.FullName} assigned two typeIds: {resultTypeContextAndTypeId.TypeId} and {typeContextAndTypeId.TypeId}.");
         }

         resultTypeContextAndTypeId = typeContextAndTypeIdByTypeId.GetOrAdd(typeContextAndTypeId.TypeId, add => typeContextAndTypeId);
         if (resultTypeContextAndTypeId.Type != typeContextAndTypeId.Type) {
            throw new InvalidStateException($"Type {typeContextAndTypeId.Type.FullName} and {resultTypeContextAndTypeId.Type.FullName} assigned to typeId {typeContextAndTypeId.TypeId}.");
         }
      }

      public class TypeContextAndTypeId {
         public TypeContextAndTypeId(int typeId, TypeContext typeContext) {
            TypeId = typeId;
            TypeContext = typeContext;
         }

         public int TypeId { get; }
         public TypeContext TypeContext { get; }
         public Type Type => TypeContext.Type;
         public Func<object> Activator => TypeContext.Activator;
      }
   }
}