using System;
using System.Collections.Concurrent;
using System.Net;
using Dargon.Commons.Exceptions;
using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier {
   /// <summary>
   /// Immutable state representing the local node's identity in a courier network.
   /// </summary>
   [VoxType((int)CourierVoxTypeIds.Identity)]
   public partial class Identity {
      public Identity() { }

      public Identity(Guid id) {
         Id = id;
      }

      public Guid Id { get; /* setter is for serialization */ internal set; }
      public string VanityName { get; set; }
      /// <summary>
      /// Remotely-owned & declared properties; can change in response to heartbeats.
      /// Can be used to declare information (e.g. UDP port for fast communication).
      ///
      /// Validated by gatekeeper when submitted, currently only submitted when we
      /// start a connection session, so validated with client handshake.
      /// </summary>
      [D<N, P>] public ConcurrentDictionary<string, object> DeclaredProperties { get; set; } = new();

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

         VanityName = identity.VanityName;
         DeclaredProperties = identity.DeclaredProperties;
      }

      public static Identity Create(Guid? forceId = null) {
         var id = forceId ?? Guid.NewGuid();
         var hostName = Dns.GetHostName();
         return new Identity(id) {
            VanityName = $"{hostName}:{id.ToString("N").Substring(8)}"
         };
      }

      public override string ToString() {
         return $"[Identity Id = {Id}, Name = {VanityName}]";
      }
   }
}