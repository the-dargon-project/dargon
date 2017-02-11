using System;
using System.Collections.Generic;

namespace Dargon.Vox {
   public abstract class VoxTypes {
      private readonly Dictionary<int, TypeContext> typesById = new Dictionary<int, TypeContext>();
      private readonly int baseId;

      protected VoxTypes(int baseId) {
         this.baseId = baseId;
      }

      protected void Register<T>(int id) => Register(id, TypeContext.Create<T>());
      protected void Register<T>(int id, Func<T> activator) => Register(id, TypeContext.Create(typeof(T), () => activator()));
      protected void Register(int id, Type type) => Register(id, TypeContext.Create(type));
      protected void Register(int id, Type type, Func<object> activator) => Register(id, TypeContext.Create(type, activator));
      protected void Register(int id, TypeContext typeContext) => typesById.Add(baseId + id, typeContext);

      public IReadOnlyDictionary<int, TypeContext> EnumerateTypes() => typesById;
   }
}