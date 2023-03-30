using Dargon.Commons;
using Dargon.Commons.Templating;
using Dargon.Courier.PeeringTier;

namespace Dargon.Courier {
   /// <summary>
   /// DO NOT ADD INSTANCE FIELDS - this class is inherited by DTOs.
   /// </summary>
   public class CourierDtoHelpers<TDto, TString_Key> where TString_Key : struct, ITemplateString {
      public static TDto GetFromIdentity(PeerContext peerContext) {
         return GetFromIdentity(peerContext.Identity);
      }

      public static TDto GetFromIdentity(Identity identity) {
         return (TDto)identity.DeclaredProperties[default(TString_Key).Value];
      }

      public static TDto SetNewForIdentity(Identity identity, TDto value) {
         identity.DeclaredProperties.ContainsKey(default(TString_Key).Value).AssertIsFalse();
         identity.DeclaredProperties[default(TString_Key).Value] = value;
         return value;
      }
   }
}