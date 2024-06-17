using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Dargon.Ryu.Internals {
   public interface IBootstrapper {
      /// <summary>
      /// Initializes the container with the provided Ryu Configuration
      /// </summary>
      Task<IRyuContainer> BootstrapAsync(RyuConfiguration configuration);
   }

   public class Bootstrapper : IBootstrapper {
      private readonly IAssemblyLoader assemblyLoader;
      private readonly IModuleLoader moduleLoader;
      private readonly IActivator activator;
      private readonly IModuleImporter moduleImporter;

      public Bootstrapper(IAssemblyLoader assemblyLoader, IModuleLoader moduleLoader, IActivator activator, IModuleImporter moduleImporter) {
         this.assemblyLoader = assemblyLoader;
         this.moduleLoader = moduleLoader;
         this.activator = activator;
         this.moduleImporter = moduleImporter;
      }

      public async Task<IRyuContainer> BootstrapAsync(RyuConfiguration configuration) {
         var assemblies = configuration.EnableAlwaysLoadModuleSearch ? assemblyLoader.LoadAssembliesFromNeighboringDirectories() : new HashSet<Assembly>();
         var modules = moduleLoader.LoadModules(configuration, assemblies);
         var container = new RyuContainer(configuration.ParentContainerOpt, activator);
         container.Name = configuration.Name ?? container.Name;
         container.Initialize();
         await moduleImporter.ImportModulesAsync(container, modules);
         return container;
      }
   }
}
