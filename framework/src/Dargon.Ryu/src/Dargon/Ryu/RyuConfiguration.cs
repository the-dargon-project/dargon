using Dargon.Ryu.Modules;
using System;
using System.Collections.Generic;

namespace Dargon.Ryu {
   public class RyuConfiguration {
      public HashSet<RyuModule> AdditionalModules { get; } = new HashSet<RyuModule>();
   }
}
