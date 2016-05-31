using Castle.Core.Internal;
using Dargon.Commons;
using Dargon.Commons.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Dargon.Commons.Collections;
using Dargon.Courier.ManagementTier.Vox;

namespace Dargon.Courier.ManagementTier {
   [Guid("776C1CEC-44DE-40CA-9ED4-0F942BC3A8DC")]
   public interface IManagementObjectService {
      IEnumerable<ManagementObjectIdentifierDto> EnumerateManagementObjects();
      ManagementObjectStateDto GetManagementObjectDescription(Guid mobId);
      object InvokeManagedOperation(string mobFullName, string methodName, object[] args);
   }

   public class ManagementObjectService : IManagementObjectService {
      private readonly ConcurrentDictionary<string, Guid> mobIdByFullName = new ConcurrentDictionary<string, Guid>();
      private readonly ConcurrentDictionary<Guid, ManagementObjectContext> mobContextById = new ConcurrentDictionary<Guid, ManagementObjectContext>();
      
      public void RegisterService(object service) {
         Guid serviceId;
         if (!service.GetType().TryGetInterfaceGuid(out serviceId)) {
            throw new InvalidOperationException($"Mob of type {service.GetType().FullName} does not have default service id.");
         }
         RegisterService(serviceId, service);
      }

      public void RegisterService(Guid mobId, object mobInstance) {
         RegisterService(mobId, mobInstance, mobInstance.GetType().FullName);
      }

      public void RegisterService(Guid mobId, object mobInstance, string mobFullName) {
         var methods = mobInstance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                  .Where(method => method.HasAttribute<ManagedOperationAttribute>());

         var methodDescriptions = new List<MethodDescriptionDto>();
         var invokableMethodsByName = new MultiValueDictionary<string, MethodInfo>();
         foreach (var method in methods) {
            methodDescriptions.Add(
               new MethodDescriptionDto {
                  Name = method.Name,
                  Parameters = method.GetParameters().Map(
                     p => new ParameterDescriptionDto { Name = p.Name, Type = p.ParameterType }),
                  ReturnType = method.ReturnType
               });
            invokableMethodsByName.Add(method.Name, method);
         }

         var properties = mobInstance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                     .Where(property => property.HasAttribute<ManagedPropertyAttribute>());
         var propertyDescriptions = new List<PropertyDescriptionDto>();
         var invokablePropertiesByName = new Dictionary<string, PropertyInfo>();
         foreach (var property in properties) {
            propertyDescriptions.Add(
               new PropertyDescriptionDto {
                  Name = property.Name,
                  Type = property.PropertyType,
                  HasGetter = property.CanRead,
                  HasSetter = property.CanWrite
               });
            invokablePropertiesByName.Add(property.Name, property);
         }

         var context = new ManagementObjectContext {
            IdentifierDto = new ManagementObjectIdentifierDto {
               FullName = mobFullName,
               Id = mobId
            },
            StateDto = new ManagementObjectStateDto {
               Methods = methodDescriptions,
               Properties = propertyDescriptions
            },
            InvokableMethodsByName = invokableMethodsByName,
            InvokablePropertiesByName = invokablePropertiesByName,
            Instance = mobInstance
         };
         mobIdByFullName.AddOrThrow(mobFullName, mobId);
         mobContextById.AddOrThrow(mobId, context);
      }
      
      public IEnumerable<ManagementObjectIdentifierDto> EnumerateManagementObjects() {
         return mobContextById.Values.Select(v => v.IdentifierDto);
      }

      public ManagementObjectStateDto GetManagementObjectDescription(Guid mobId) {
         return mobContextById[mobId].StateDto;
      }

      public object InvokeManagedOperation(string mobFullName, string methodName, object[] args) {
         var mobContext = mobContextById[mobIdByFullName[mobFullName]];
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

         return method.Invoke(mobContext.Instance, args);
      }

      private class ManagementObjectContext {
         public ManagementObjectIdentifierDto IdentifierDto { get; set; }
         public ManagementObjectStateDto StateDto { get; set; }
         public MultiValueDictionary<string, MethodInfo> InvokableMethodsByName { get; set; }
         public Dictionary<string, PropertyInfo> InvokablePropertiesByName { get; set; }
         public object Instance { get; set; }
      }
   }
}
