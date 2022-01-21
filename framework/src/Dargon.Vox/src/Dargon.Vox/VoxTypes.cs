using System;
using System.Collections.Generic;

namespace Dargon.Vox {
   public abstract class VoxTypes {
      private readonly Dictionary<int, TypeContext> typesById = new Dictionary<int, TypeContext>();
      private readonly int baseId;
      private readonly int? reservedIdCount;

      /// <param name="baseId">Reserve in VoxTypeIdAssignments.txt</param>
      /// <param name="reservedIdCount">How many IDs starting at baseId are reserved.</param>
      protected VoxTypes(int baseId, int? reservedIdCount = null) {
         this.baseId = baseId;
         this.reservedIdCount = reservedIdCount;
      }

      protected void Register<T>(int id) => Register(id, TypeContext.Create<T>());
      protected void Register<T>(int id, Func<T> activator) => Register(id, TypeContext.Create(typeof(T), () => activator()));
      protected void Register(int id, Type type) => Register(id, TypeContext.Create(type));
      protected void Register(int id, Type type, Func<object> activator) => Register(id, TypeContext.Create(type, activator));
      protected void Register(int id, TypeContext typeContext) => typesById.Add(baseId + VerifyIdOffset(id), typeContext);

      private int VerifyIdOffset(int idOffset) {
         if (reservedIdCount.HasValue && idOffset >= reservedIdCount) {
            throw new ArgumentOutOfRangeException($"ID Offset is {idOffset} but reservation is in range [{baseId}, {baseId + reservedIdCount.Value}).");
         }

         return idOffset;
      }

      public IReadOnlyDictionary<int, TypeContext> EnumerateTypes() => typesById;
   }
}