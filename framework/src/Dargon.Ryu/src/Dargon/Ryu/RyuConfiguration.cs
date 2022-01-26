using Dargon.Ryu.Modules;
using System;
using System.Collections.Generic;

namespace Dargon.Ryu {
   public class RyuConfiguration {
      public bool EnableAlwaysLoadModuleSearch { get; set; }

      public IRyuContainer ParentContainerOpt { get; set; }

      /// <summary>
      /// For specifying manually loaded modules to load with init.
      /// </summary>
      public HashSet<RyuModule> AdditionalModules { get; } = new HashSet<RyuModule>();
   }
}
