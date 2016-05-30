using System;
using System.Net;
using Dargon.Vox;

namespace Dargon.Courier {
   /// <summary>
   /// Immutable state representing the local node's identity in a courier network.
   /// </summary>
   [AutoSerializable]
   public class Identity {
      public Guid Id { get; set; }
      public string Name { get; set; }

      public bool Matches(Guid id, IdentityMatchingScope matchingScope) {
         bool result = false;
         if (matchingScope >= IdentityMatchingScope.LocalIdentity) {
            result |= id == Id;
         }
         if (matchingScope >= IdentityMatchingScope.Broadcast) {
            result |= id == Guid.Empty;
         }
         return result;
      }

      public void Update(Identity identity) {
         Id = identity.Id;
         Name = identity.Name;
      }

      public static Identity Create() {
         var id = Guid.NewGuid();
         var hostName = Dns.GetHostName();
         return new Identity {
            Id = id,
            Name = $"{hostName}:{id.ToString("N").Substring(8)}"
         };
      }

      public override string ToString() => $"[Identity Id = {Id}, Name = {Name}]";
   }
}