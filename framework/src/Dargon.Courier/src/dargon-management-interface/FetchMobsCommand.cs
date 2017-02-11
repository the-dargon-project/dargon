using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Courier.ManagementTier;
using Dargon.Repl;

namespace Dargon.Courier.Management.UI {
   public class FetchMobsCommand : ICommand {
      public string Name => "fetch-mobs";

      public int Eval(string args) {
         var mobs = ReplGlobals.ManagementObjectService.EnumerateManagementObjects();

         var root = new SomeNode();

         foreach (var mob in mobs) {
            var breadcrumbs = mob.FullName.Split('.', '/');
            var current = root;
            foreach (var breadcrumb in breadcrumbs) {
               current = current.GetOrAddChild(breadcrumb);
            }
            current.MobDto = mob;
         }

         ReplGlobals.Root = root;
         return 0;
      }
   }
}
