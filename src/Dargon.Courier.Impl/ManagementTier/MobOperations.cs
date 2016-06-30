using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dargon.Commons.Collections;
using Dargon.Courier.ManagementTier.Vox;
using Dargon.Commons;
using Dargon.Courier.Utilities;

namespace Dargon.Courier.ManagementTier {
   public class MobOperations {
      private readonly MobContextFactory mobContextFactory;
      private readonly MobContextContainer mobContextContainer;

      public MobOperations(MobContextFactory mobContextFactory, MobContextContainer mobContextContainer) {
         this.mobContextFactory = mobContextFactory;
         this.mobContextContainer = mobContextContainer;
      }

      public void RegisterMob(Guid mobId, object mobInstance, string mobFullName) {
         var mobContext = mobContextFactory.Create(mobId, mobInstance, mobFullName);
         mobContextContainer.Add(mobContext);
      }

      public Task<object> InvokeManagedOperationAsync(string mobFullName, string methodName, object[] args) {
         var mobContext = mobContextContainer.Get(mobFullName);
         MethodInfo method = mobContext.InvokableMethodsByName.GetValueOrDefault(methodName)
                                      ?.FirstOrDefault(m => m.GetParameters().Length == args.Length);
         if (method == null) {
            var property = mobContext.InvokablePropertiesByName[methodName];
            if (args.Any()) {
               method = property.GetSetMethod();
            } else {
               method = property.GetGetMethod();
            }
         }

         return TaskUtilities.UnboxValueIfTaskAsync(method.Invoke(mobContext.Instance, args));
      }
   }
}
