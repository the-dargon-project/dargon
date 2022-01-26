using System;
using Dargon.Ryu.Internals;

namespace Dargon.Ryu {
   public interface IRyuFacade : IRyuContainer {
      IRyuContainer Container { get; }

      public IActivator Activator { get; }
      object Activate(Type type);
      
      public IModuleImporter ModuleImporter { get; }
   }
}
