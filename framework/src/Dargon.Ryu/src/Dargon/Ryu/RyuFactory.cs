using Dargon.Ryu.Internals;
using Dargon.Ryu.Logging;

namespace Dargon.Ryu {
   public class RyuFactory {
      private readonly IRyuLogger logger;

      public RyuFactory() : this(new RyuConsoleLoggerImpl()) { }

      public RyuFactory(IRyuLogger logger) {
         this.logger = logger;
      }

      public IRyuFacade Create() {
         return Create(new RyuConfiguration());
      }

      public IRyuFacade Create(RyuConfiguration configuration) {
         IAssemblyLoader assemblyLoader = new AssemblyLoader(logger);
         IModuleLoader moduleLoader = new ModuleLoader();
         IActivator activator = new Activator();
         IModuleSorter moduleSorter = new ModuleSorter();
         IModuleImporter moduleImporter = new ModuleImporter(moduleSorter);
         var bootstrapper = new Bootstrapper(assemblyLoader, moduleLoader, activator, moduleImporter);
         var container = bootstrapper.Bootstrap(configuration);
         var facade = new RyuFacade(container, activator);
         facade.Initialize();
         return facade;
      }
   }
}