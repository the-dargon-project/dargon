using System;
using System.Collections.Generic;
using Dargon.Commons.Collections;

namespace Dargon.Courier.ManagementTier {
   public class MobContextContainer {
      private readonly ConcurrentDictionary<string, Guid> mobIdByFullName = new ConcurrentDictionary<string, Guid>();
      private readonly ConcurrentDictionary<Guid, MobContext> mobContextById = new ConcurrentDictionary<Guid, MobContext>();

      public void Add(MobContext context) {
         mobIdByFullName.AddOrThrow(context.IdentifierDto.FullName, context.IdentifierDto.Id);
         mobContextById.AddOrThrow(context.IdentifierDto.Id, context);
      }

      public IEnumerable<MobContext> Enumerate() {
         return mobContextById.Values;
      }

      public MobContext Get(string mobFullName) {
         return mobContextById[mobIdByFullName[mobFullName]];
      }

      public MobContext Get(Guid mobId) {
         return mobContextById[mobId];
      }
   }
}
