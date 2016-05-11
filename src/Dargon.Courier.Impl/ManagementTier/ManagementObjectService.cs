using Castle.Core.Internal;
using Dargon.Commons;
using Dargon.Commons.Utilities;
using Dargon.Vox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Dargon.Commons.Collections;

namespace Dargon.Courier.ManagementTier {
   [Guid("776C1CEC-44DE-40CA-9ED4-0F942BC3A8DC")]
   public interface IManagementObjectService {
      IEnumerable<ManagementObjectIdentifierDto> EnumerateManagementObjects();
      ManagementObjectStateDto GetManagementObjectDescription(Guid mobId);
      object InvokeManagedOperation(string mobFullName, string methodName, object[] args);
   }

   public class ManagementObjectDescription {
      public Guid Id { get; set; }
      public string Name { get; set; }
      public Type Type { get; set; }
      public object Instance { get; set; }
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

         var context = new ManagementObjectContext {
            IdentifierDto = new ManagementObjectIdentifierDto {
               FullName = mobFullName,
               Id = mobId
            },
            StateDto = new ManagementObjectStateDto {
               Methods = methodDescriptions
            },
            InvokableMethodsByName = invokableMethodsByName,
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
         var method = mobContext.InvokableMethodsByName.Get(methodName)
                                .First(m => m.GetParameters().Length == args.Length);
         return method.Invoke(mobContext.Instance, args);
      }

      private class ManagementObjectContext {
         public ManagementObjectIdentifierDto IdentifierDto { get; set; }
         public ManagementObjectStateDto StateDto { get; set; }
         public MultiValueDictionary<string, MethodInfo> InvokableMethodsByName { get; set; }
         public object Instance { get; set; }
      }
   }
   
   [AutoSerializable]
   public class ManagementObjectIdentifierDto {
      public Guid Id { get; set; }
      public string FullName { get; set; }
   }

   [AutoSerializable]
   public class ManagementObjectStateDto {
      public IReadOnlyList<MethodDescriptionDto> Methods { get; set; }
   }

   [AutoSerializable]
   public class MethodDescriptionDto {
      public string Name { get; set; }
      public IReadOnlyList<ParameterDescriptionDto> Parameters { get; set; }
      public Type ReturnType { get; set; }
   }

   [AutoSerializable]
   public class ParameterDescriptionDto {
      public string Name { get; set; }
      public Type Type { get; set; }
   }
}
