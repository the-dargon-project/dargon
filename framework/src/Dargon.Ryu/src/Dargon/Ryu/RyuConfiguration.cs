using Dargon.Ryu.Modules;
using System;
using System.Collections.Generic;

namespace Dargon.Ryu {
   public class RyuConfiguration {
      public LoadingStrategyFlags LoadingStrategy { get; set; } = LoadingStrategyFlags.Default;
      public HashSet<RyuModule> AdditionalModules { get; } = new HashSet<RyuModule>();
      public HashSet<Type> ExcludedModuleTypes { get; } = new HashSet<Type>();
   }

   [Flags]
   public enum LoadingStrategyFlags {
      None,
      DisableDirectoryAssemblyLoading,
      LazyLoadModules,
      Default = None
   }
}
