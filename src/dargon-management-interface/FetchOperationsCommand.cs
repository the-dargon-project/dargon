using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Repl;

namespace Dargon.Courier.Management.UI {
   public class FetchOperationsCommand : ICommand {
      public string Name => "fetch-ops";

      public int Eval(string args) {
         var mob = ReplGlobals.Current?.MobDto;
         if (mob == null) {
            throw new Exception("Mob not specified.");
         }
         var desc = ReplGlobals.ManagementObjectService.GetManagementObjectDescription(mob.Id);
         foreach (var method in desc.Methods) {
            var methodNode = ReplGlobals.Current.GetOrAddChild(method.Name);
            methodNode.MethodDto = method;
         }
         foreach (var property in desc.Properties) {
            var propertyNode = ReplGlobals.Current.GetOrAddChild(property.Name);
            propertyNode.PropertyDto = property;
         }
         return 0;
      }
   }
}
