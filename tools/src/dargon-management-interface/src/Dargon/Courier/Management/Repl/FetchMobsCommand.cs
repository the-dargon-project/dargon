using Dargon.Repl;

namespace Dargon.Courier.Management.Repl {
   public class FetchMobsCommand : ICommand {
      public string Name => "fetch-mobs";

      public int Eval(string args) {
         var mobs = ReplGlobals.ManagementObjectService.EnumerateManagementObjects();

         var root = new SomeNode { Name = "" };

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
