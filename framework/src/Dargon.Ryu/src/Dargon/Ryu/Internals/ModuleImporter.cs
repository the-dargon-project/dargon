using Dargon.Commons;
using Dargon.Ryu.Extensibility;
using Dargon.Ryu.Modules;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dargon.Ryu.Internals {
   public interface IModuleImporter {
      void ImportModules(RyuContainer container, IReadOnlyList<IRyuModule> modules);
   }

   public class ModuleImporter : IModuleImporter {
      private readonly IModuleSorter moduleSorter;

      public ModuleImporter(IModuleSorter moduleSorter) {
         this.moduleSorter = moduleSorter;
      }

      public void ImportModules(RyuContainer container, IReadOnlyList<IRyuModule> modules) {
         var extensionArguments = new RyuExtensionArguments { Container = container };

         // Order extensions by invocation order
         var extensions = modules.OfType<IRyuExtensionModule>().ToList();
         var orderedExtensions = moduleSorter.SortModulesByInitializationOrder(extensions).Cast<IRyuExtensionModule>().ToArray();
         InvokeInitializeDeprecated(orderedExtensions);

         // Initialiaze container
         var ryuTypesByType = GetTypeToRyuTypeMap(modules);
         var typeToImplementors = GetTypeToImplementorsMap(ryuTypesByType);
         container.Import(ryuTypesByType, typeToImplementors);
         orderedExtensions.ForEach(e => e.Loaded(extensionArguments));

         // Construct container contents
         orderedExtensions.ForEach(e => e.PreConstruction(extensionArguments));
         var objectsByConstructionOrder = ConstructRequiredTypes(container, ryuTypesByType);
         orderedExtensions.ForEach(e => e.PostConstruction(extensionArguments));

         // Initialize container contents - I have no clue why this would be a useful second pass after construction,
         // it seems like a code smell.
         // Edit: This is probably only useful if we want to run initialization logic after fields are loaded after ctor
         // executes, or if we're doing unit testing and want constructors to be side-effect-free. Leaving in for now.
         orderedExtensions.ForEach(e => e.PreInitialization(extensionArguments));
         InvokeInitializeDeprecated(objectsByConstructionOrder);
         orderedExtensions.ForEach(e => e.PostInitialization(extensionArguments));
      }

      private static Dictionary<Type, RyuType> GetTypeToRyuTypeMap(IReadOnlyList<IRyuModule> modules) {
         return modules.SelectMany(m => m.TypeInfoByType.Values)
                       .ToDictionary(rt => rt.Type);
      }

      private static ConcurrentDictionary<Type, HashSet<RyuType>> GetTypeToImplementorsMap(Dictionary<Type, RyuType> typeToRyuType) {
         var typeToImplementors = new ConcurrentDictionary<Type, HashSet<RyuType>>();
         foreach (var type in typeToRyuType) {
            GetTypeToImplementorsMapIterateHelper(typeToImplementors, type.Value, type.Key);
         }
         return typeToImplementors;
      }

      private static void GetTypeToImplementorsMapIterateHelper(ConcurrentDictionary<Type, HashSet<RyuType>> typeToImplementors, RyuType ryuType, Type type) {
         var currentType = type;
         while (currentType != null) {
            GetTypeToImplementorsMapImplementsHelper(typeToImplementors, ryuType, currentType);
            currentType = currentType.GetTypeInfo().BaseType;
         }
         foreach (var implementedInterface in type.GetTypeInfo().GetInterfaces()) {
            GetTypeToImplementorsMapImplementsHelper(typeToImplementors, ryuType, implementedInterface);
         }
      }

      private static void GetTypeToImplementorsMapImplementsHelper(ConcurrentDictionary<Type, HashSet<RyuType>> typeToImplementors, RyuType ryuType, Type currentType) {
         typeToImplementors.AddOrUpdate(
            currentType,
            add => new HashSet<RyuType> { ryuType },
            (update, existing) => existing.Tap(e => e.Add(ryuType)));
      }

      private static List<object> ConstructRequiredTypes(RyuContainer container, Dictionary<Type, RyuType> ryuTypesByType) {
         List<object> objectsByConstructionOrder = new List<object>();
         container.ObjectActivated += objectsByConstructionOrder.Add;
         foreach (var type in ryuTypesByType.Values) {
            if (type.IsRequired()) {
               container.GetOrActivate(type.Type);
            }
         }
         container.ObjectActivated -= objectsByConstructionOrder.Add;
         return objectsByConstructionOrder;
      }

      public void InvokeInitializeDeprecated<T>(params T[] objs) {
         foreach (var obj in objs) {
            var type = obj.GetType();
            var initialize = type.GetTypeInfo().GetMethod("Initialize", BindingFlags.Public);
            initialize?.Invoke(obj, null);
         }
      }

      public class RyuExtensionArguments : IRyuExtensionArguments {
         public IRyuContainer Container { get; set; }
      }
   }
}