using System;
using System.Threading.Tasks;
using Dargon.Ryu.Internals;
using Dargon.Ryu.Logging;
using Activator = Dargon.Ryu.Internals.Activator;

namespace Dargon.Ryu {
   public class RyuFactory {
      private readonly IRyuLogger logger;

      public RyuFactory() : this(new RyuConsoleLoggerImpl()) { }

      public RyuFactory(IRyuLogger logger) {
         this.logger = logger;
      }

      public IRyuContainer Create(string name) {
         return Create(new RyuConfiguration { Name = name });
      }

      [Obsolete]
      public IRyuContainer Create(RyuConfiguration configuration) => CreateAsync(configuration).Result;

      public async Task<IRyuContainer> CreateAsync(RyuConfiguration configuration) {
         IAssemblyLoader assemblyLoader = new AssemblyLoader(logger);
         IModuleLoader moduleLoader = new ModuleLoader();
         IActivator activator = new Activator(logger);
         IModuleSorter moduleSorter = new ModuleSorter();
         IModuleImporter moduleImporter = new ModuleImporter(moduleSorter);
         var bootstrapper = new Bootstrapper(assemblyLoader, moduleLoader, activator, moduleImporter);
         var container = await bootstrapper.BootstrapAsync(configuration);
         var facade = new RyuFacade(container, activator, moduleImporter);
         facade.Initialize();
         return container;
      }
   }
}