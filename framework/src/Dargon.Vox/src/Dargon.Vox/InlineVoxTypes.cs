using System.Collections.Generic;

namespace Dargon.Vox {
   public class InlineVoxTypes : VoxTypes {
      public InlineVoxTypes(int baseId, IReadOnlyDictionary<int, TypeContext> typeContextsById) : base(baseId) {
         foreach (var kvp in typeContextsById) {
            Register(kvp.Key, kvp.Value);
         }
      }
   }
}