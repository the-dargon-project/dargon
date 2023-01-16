using System.Collections.Generic;
using Dargon.Vox;

namespace Dargon.Courier.PeeringTier;

[AutoSerializable]
public class WhoamiDto {
   public Identity Identity { get; set; }
   public Dictionary<string, object> AdditionalParameters { get; set; }
}