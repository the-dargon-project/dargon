using Dargon.Commons;
using Dargon.Ryu.Modules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dargon.Ryu.Internals {
   public interface IModuleLoader {
      IReadOnlyList<IRyuModule> LoadModules(RyuConfiguration configuration);
   }

   public class ModuleLoader : IModuleLoader {
      public IReadOnlyList<IRyuModule> LoadModules(RyuConfiguration configuration) {
         var modules = new List<IRyuModule>();
         foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            var moduleTypes = assembly.GetTypes().Where(RyuModuleTypeFilter(configuration));
            var moduleInstances = moduleTypes.Select(System.Activator.CreateInstance);
            modules.AddRange(moduleInstances.Cast<IRyuModule>());
         }
         modules.AddRange(configuration.AdditionalModules);
         return modules;
      }

      private Func<Type, bool> RyuModuleTypeFilter(RyuConfiguration configuration) {
         return (type) => {
            var moduleConfiguration = type.GetAttributeOrNull<ModuleConfigurationAttribute>();
            if (configuration.ExcludedModuleTypes.Contains(type) ||
                moduleConfiguration.IsManualLoadRequired()) {
               return false;
            } else {
               var moduleType = typeof(IRyuModule);
               return moduleType.IsAssignableFrom(type) && !type.IsAbstract;
            }
         };
      }
   }
}
