using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Dargon.Commons;
using Dargon.Commons.Collections;
using Dargon.Commons.Utilities;
using Dargon.Courier.AuditingTier;
using Dargon.Courier.AuditingTier.Utilities;
using Dargon.Courier.ManagementTier.Vox;
using Dargon.Courier.Utilities;
using Dargon.Vox.Utilities;
using static Dargon.Commons.Channels.ChannelsExtensions;

namespace Dargon.Courier.ManagementTier {
   public class MobContextFactory {
      private delegate object VisitorFunc(AuditService auditService, string name);

      private readonly IGenericFlyweightFactory<VisitorFunc> getAggregatorVisitors
         = GenericFlyweightFactory.ForMethod<VisitorFunc>(
            typeof(MobContextFactory),
            nameof(VisitGetAggregator));

      private delegate IDataPointCircularBuffer HandleCreatePeriodicSetFunc(object mobInstance, PropertyInfo property, string dataSetName, AuditService auditService);

      private readonly IGenericFlyweightFactory<HandleCreatePeriodicSetFunc> handleCreatePeriodicSetVisitors
         = GenericFlyweightFactory.ForMethod<HandleCreatePeriodicSetFunc>(
            typeof(MobContextFactory),
            nameof(HandleCreatePeriodicSet));

      private static object VisitGetAggregator<T>(AuditService auditService, string name) {
         return auditService.GetAggregator<T>(name);
      }

      private readonly AuditService auditService;

      public MobContextFactory(AuditService auditService) {
         this.auditService = auditService;
      }

      public MobContext Create(Guid mobId, object mobInstance, string mobFullName) {
         MultiValueDictionary<string, MethodInfo> invokableMethodsByName;
         var methodDescriptions = GetMethods(mobInstance, out invokableMethodsByName);

         Dictionary<string, PropertyInfo> invokablePropertiesByName;
         var propertyDescriptions = GetProperties(mobInstance, out invokablePropertiesByName);

         Dictionary<string, IDataPointCircularBuffer> dataSetBuffersByAlias;
         var dataSetDescriptions = GetAndConfigureDataSetBuffersMap(mobInstance, mobFullName, out dataSetBuffersByAlias);

         return new MobContext {
            IdentifierDto = new ManagementObjectIdentifierDto {
               FullName = mobFullName,
               Id = mobId
            },
            StateDto = new ManagementObjectStateDto {
               Methods = methodDescriptions,
               Properties = propertyDescriptions,
               DataSets = dataSetDescriptions
            },
            InvokableMethodsByName = invokableMethodsByName,
            InvokablePropertiesByName = invokablePropertiesByName,
            DataSetBuffersByAlias = dataSetBuffersByAlias,
            Instance = mobInstance
         };
      }

      private List<DataSetDescriptionDto> GetAndConfigureDataSetBuffersMap(object mobInstance, string mobFullName, out Dictionary<string, IDataPointCircularBuffer> dataSetBuffersByAlias) {
         var dataSetDescriptions = new List<DataSetDescriptionDto>();
         dataSetBuffersByAlias = new Dictionary<string, IDataPointCircularBuffer>();
         foreach (var dataSetAttribute in mobInstance.GetType().GetCustomAttributes<ManagedDataSetAttribute>()) {
            // make the audit service create the given data set
            if (dataSetAttribute.Type == typeof(IAuditCounter)) {
               auditService.GetCounter(dataSetAttribute.LongName);
            } else if (dataSetAttribute.Type.IsGenericType && dataSetAttribute.Type.GetGenericTypeDefinition() == typeof(IAuditAggregator<>)) {
               var elementType = dataSetAttribute.Type.GetGenericArguments()[0];
               getAggregatorVisitors.Get(elementType)(auditService, dataSetAttribute.LongName);
            }

            // get dataset buffer associated with dataset
            var dataSetBuffer = auditService.GetDataSetBuffer(dataSetAttribute.LongName);
            dataSetBuffersByAlias.Add(dataSetAttribute.Alias, dataSetBuffer);
            dataSetDescriptions.Add(
               new DataSetDescriptionDto {
                  Name = dataSetAttribute.Alias,
                  ElementType = dataSetBuffer.ElementType
               });
         }

         foreach (var property in GetManagedProperties(mobInstance)) {
            var managedPropertyAttribute = property.GetAttribute<ManagedPropertyAttribute>();
            if (managedPropertyAttribute.IsDataSource) {
               var dataSetName = $"__{mobFullName}.{property.Name}";
               var dataSetBuffer = handleCreatePeriodicSetVisitors.Get(property.PropertyType)(mobInstance, property, dataSetName, auditService);
               dataSetBuffersByAlias.Add(property.Name, dataSetBuffer);
               dataSetDescriptions.Add(
                  new DataSetDescriptionDto {
                     Name = property.Name,
                     ElementType = dataSetBuffer.ElementType
                  });
            }
         }
         return dataSetDescriptions;
      }

      private static List<PropertyDescriptionDto> GetProperties(object mobInstance, out Dictionary<string, PropertyInfo> invokablePropertiesByName) {
         var properties = GetManagedProperties(mobInstance);
         var propertyDescriptions = new List<PropertyDescriptionDto>();
         invokablePropertiesByName = new Dictionary<string, PropertyInfo>();
         foreach (var property in properties) {
            var managedPropertyAttribute = property.GetAttribute<ManagedPropertyAttribute>();

            propertyDescriptions.Add(
               new PropertyDescriptionDto {
                  Name = property.Name,
                  Type = property.PropertyType,
                  HasGetter = property.CanRead,
                  HasSetter = property.CanWrite
               });
            invokablePropertiesByName.Add(property.Name, property);
         }
         return propertyDescriptions;
      }

      private static PropertyInfo[] GetManagedProperties(object mobInstance) {
         var properties = mobInstance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                     .Where(property => property.HasAttribute<ManagedPropertyAttribute>())
                                     .ToArray();
         return properties;
      }

      private static List<MethodDescriptionDto> GetMethods(object mobInstance, out MultiValueDictionary<string, MethodInfo> invokableMethodsByName) {
         var methods = mobInstance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                  .Where(method => method.HasAttribute<ManagedOperationAttribute>())
                                  .ToArray();

         var methodDescriptions = new List<MethodDescriptionDto>();
         invokableMethodsByName = new MultiValueDictionary<string, MethodInfo>();
         foreach (var method in methods) {
            var returnType = TaskUtilities.UnboxTypeIfTask(method.ReturnType);

            methodDescriptions.Add(
               new MethodDescriptionDto {
                  Name = method.Name,
                  Parameters = method.GetParameters().Map(
                     p => new ParameterDescriptionDto { Name = p.Name, Type = p.ParameterType }),
                  ReturnType = returnType
               });
            invokableMethodsByName.Add(method.Name, method);
         }
         return methodDescriptions;
      }

      private static IDataPointCircularBuffer HandleCreatePeriodicSet<T>(object mobInstance, PropertyInfo property, string dataSetName, AuditService auditService) {
         return auditService.CreatePeriodicDataSet<T>(dataSetName, () => (T)property.GetValue(mobInstance));
      }
   }
}