﻿using Dargon.Ryu.Internals;
using Dargon.Ryu.Logging;

namespace Dargon.Ryu {
   public class RyuFactory {
      private readonly IRyuLogger logger;

      public RyuFactory() : this(new RyuConsoleLoggerImpl()) { }

      public RyuFactory(IRyuLogger logger) {
         this.logger = logger;
      }

      public IRyuContainer Create() {
         return Create(new RyuConfiguration());
      }

      public IRyuContainer Create(RyuConfiguration configuration) {
         IAssemblyLoader assemblyLoader = new AssemblyLoader(logger);
         IModuleLoader moduleLoader = new ModuleLoader();
         IActivator activator = new Activator(logger);
         IModuleSorter moduleSorter = new ModuleSorter();
         IModuleImporter moduleImporter = new ModuleImporter(moduleSorter);
         var bootstrapper = new Bootstrapper(assemblyLoader, moduleLoader, activator, moduleImporter);
         var container = bootstrapper.Bootstrap(configuration);
         var facade = new RyuFacade(container, activator, moduleImporter);
         facade.Initialize();
         return container;
      }
   }
}