namespace Dargon.Ryu.Internals {
   public interface IBootstrapper {
      /// <summary>
      /// Initializes the container with the provided Ryu Configuration
      /// </summary>
      IRyuContainer Bootstrap(RyuConfiguration configuration);
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

      public IRyuContainer Bootstrap(RyuConfiguration configuration) {
         var assemblies = assemblyLoader.LoadAssembliesFromNeighboringDirectories();
         var modules = moduleLoader.LoadModules(configuration, assemblies);
         var container = new RyuContainer(null, activator);
         moduleImporter.ImportModules(container, modules);
         return container;
      }
   }
}
