using System.Collections.Generic;
using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.PeeringTier;

[VoxType((int)CourierVoxTypeIds.WhoamiDto)]
public partial class WhoamiDto {
   public Identity Identity { get; set; }
   [D<N, P>] public Dictionary<string, object> AdditionalParameters { get; set; }
}