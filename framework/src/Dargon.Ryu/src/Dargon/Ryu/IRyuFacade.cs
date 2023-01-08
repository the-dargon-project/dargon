using System;
using Dargon.Ryu.Internals;

namespace Dargon.Ryu {
   public interface IRyuFacade {
      IRyuContainer Container { get; }
      IActivator Activator { get; }
      IModuleImporter ModuleImporter { get; }
   }
}
