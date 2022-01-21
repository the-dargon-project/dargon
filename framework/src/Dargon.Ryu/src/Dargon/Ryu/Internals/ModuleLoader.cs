using Dargon.Commons;
using Dargon.Ryu.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Dargon.Commons.Collections;

namespace Dargon.Ryu.Internals {
   public interface IModuleLoader {
      IReadOnlyList<IRyuModule> LoadModules(RyuConfiguration configuration, IReadOnlySet<Assembly> assemblies);
   }

   public class ModuleLoader : IModuleLoader {
      public IReadOnlyList<IRyuModule> LoadModules(RyuConfiguration configuration, IReadOnlySet<Assembly> assemblies) {
         var modules = new List<IRyuModule>();
         foreach (var assembly in assemblies) {
            var moduleTypes = assembly.GetTypes().Where(IsLoadableRyuModuleType);
            var moduleInstances = moduleTypes.Select(System.Activator.CreateInstance);
            modules.AddRange(moduleInstances.Cast<IRyuModule>().Where(m => m.IsAutomaticLoadEnabled()));
         }
         modules.AddRange(configuration.AdditionalModules);
         return modules;
      }

      private static bool IsLoadableRyuModuleType(Type type) {
         var moduleType = typeof(IRyuModule);
         return moduleType.GetTypeInfo().IsAssignableFrom(type) &&
                !type.GetTypeInfo().IsAbstract &&
                type != typeof(LambdaRyuModule);
      }
   }
}
