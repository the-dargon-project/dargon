using Castle.Core.Internal;
using Dargon.Commons;
using Dargon.Commons.Collections;
using Dargon.Commons.Utilities;
using Dargon.Courier.ServiceTier.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dargon.Courier.ManagementTier {
   public class ManagementObjectRegistry {
      private readonly ConcurrentDictionary<Guid, object> managementObjectsByIds = new ConcurrentDictionary<Guid, object>();
      private readonly LocalServiceRegistry localServiceRegistry;

      public ManagementObjectRegistry(LocalServiceRegistry localServiceRegistry) {
         this.localServiceRegistry = localServiceRegistry;
      }

      public void RegisterService(object service) {
         Guid serviceId;
         if (!service.GetType().TryGetInterfaceGuid(out serviceId)) {
            throw new InvalidOperationException($"Mob of type {service.GetType().FullName} does not have default service id.");
         }
         RegisterService(serviceId, service);
      }

      public void RegisterService(Guid serviceId, object service) {
         localServiceRegistry.RegisterService(serviceId, service);
         managementObjectsByIds.TryAdd(serviceId, service);
      }

      public IEnumerable<Guid> EnumerateManagementObjectIds() {
         return managementObjectsByIds.Keys;
      }

      public ManagementObjectDescriptionDto GetManagementObjectDescription(Guid managementObjectId) {
         var managementObject = managementObjectsByIds[managementObjectId];
         var methods = new List<MethodDescriptionDto>();
         foreach (var method in managementObject.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                                .Where(method => method.HasAttribute<ManagedOperationAttribute>())) {
            methods.Add(
               new MethodDescriptionDto {
                  Name = method.Name,
                  Parameters = method.GetParameters().Map(
                     p => new ParameterDescriptionDto { Name = p.Name, Type = p.ParameterType }),
                  ReturnType = method.ReturnType
               });
         }
         return new ManagementObjectDescriptionDto {
            Methods = methods
         };
      }
   }
}