using System.Collections.Generic;
using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.PeeringTier;

[VoxType((int)CourierVoxTypeIds.WhoamiDto)]
public class WhoamiDto {
   public Identity Identity { get; set; }
   public Dictionary<string, object> AdditionalParameters { get; set; }
}