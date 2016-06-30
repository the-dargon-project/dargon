using System;
using System.Net;
using Dargon.Commons.Exceptions;
using Dargon.Vox;

namespace Dargon.Courier {
   /// <summary>
   /// Immutable state representing the local node's identity in a courier network.
   /// </summary>
   [AutoSerializable]
   public class Identity {
      public Identity() { }

      public Identity(Guid id) {
         Id = id;
      }

      public Guid Id { get; private set; }
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
         if (Id != identity.Id) {
            throw new InvalidStateException($"Identity Update Id Mismatch: {identity} vs {Id}");
         }
         Name = identity.Name;
      }

      public static Identity Create(Guid? forceId = null) {
         var id = forceId ?? Guid.NewGuid();
         var hostName = Dns.GetHostName();
         return new Identity(id) {
            Name = $"{hostName}:{id.ToString("N").Substring(8)}"
         };
      }

      public override string ToString() => $"[Identity Id = {Id}, Name = {Name}]";
   }
}