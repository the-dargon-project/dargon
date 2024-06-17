using Dargon.Commons;
using Dargon.Ryu.Extensibility;
using Dargon.Ryu.Modules;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.AsyncAwait;
using Dargon.Commons.AsyncPrimitives;

namespace Dargon.Ryu.Internals {
   public interface IModuleImporter {
      Task ImportModulesAsync(RyuContainer container, IReadOnlyList<IRyuModule> modules);
   }

   public class ModuleImporter : IModuleImporter {
      private static bool EnableDebugLogging { get; set; } = false;

      private readonly IModuleSorter moduleSorter;

      public ModuleImporter(IModuleSorter moduleSorter) {
         this.moduleSorter = moduleSorter;
      }

      public async Task ImportModulesAsync(RyuContainer container, IReadOnlyList<IRyuModule> modules) {
         var extensionArguments = new RyuExtensionArguments { Container = container };

         // Order extensions by invocation order
         var extensions = modules.OfType<IRyuExtensionModule>().ToList();
         var orderedExtensions = moduleSorter.SortModulesByInitializationOrder(extensions).Cast<IRyuExtensionModule>().ToList();
         //InvokeInitializeDeprecated(orderedExtensions);

         // Initialiaze container
         var ryuTypesByType = GetTypeToRyuTypeMap(modules);
         var typeToImplementors = GetTypeToImplementorsMap(ryuTypesByType);
         container.Import(ryuTypesByType, typeToImplementors);
         orderedExtensions.ForEach(e => e.Loaded(extensionArguments));

         // Construct container contents
         orderedExtensions.ForEach(e => e.PreConstruction(extensionArguments));
         await ConstructEventualAndRequiredTypesAndWaitForRequiredTypesAsync(container, ryuTypesByType);
         orderedExtensions.ForEach(e => e.PostConstruction(extensionArguments));

         // Initialize container contents - I have no clue why this would be a useful second pass after construction,
         // it seems like a code smell.
         // Edit: This is probably only useful if we want to run initialization logic after fields are loaded after ctor
         // executes, or if we're doing unit testing and want constructors to be side-effect-free. Leaving in for now.
         // Edit: This was completely unused and stopped making sense once I supported async initialization, so removed.
         // A future version might invoke Initialize on a per-object basis after that object's construction, rather than
         // performing initialization in two phases.
         //orderedExtensions.ForEach(e => e.PreInitialization(extensionArguments));
         //InvokeInitializeDeprecated(objectsByConstructionOrder);
         //orderedExtensions.ForEach(e => e.PostInitialization(extensionArguments));
      }

      private static Dictionary<Type, RyuType> GetTypeToRyuTypeMap(IReadOnlyList<IRyuModule> modules) {
         var res = new Dictionary<Type, RyuType>();
         foreach (var m in modules) {
            foreach (var (type, typeInfo) in m.TypeInfoByType) {
               if (res.TryGetValue(type, out var existing)) {
                  existing.Merge(typeInfo);
               } else {
                  res[type] = typeInfo;
               }
            }
         }
         return res;
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

      private static async Task ConstructEventualAndRequiredTypesAndWaitForRequiredTypesAsync(RyuContainer container, Dictionary<Type, RyuType> ryuTypesByType) {
         var ic = container.I;

         RyuSynchronizationContexts syncContextsOrNull = null;
         if (ryuTypesByType.TryGetValue(typeof(RyuSynchronizationContexts), out var rscrt)) {
            syncContextsOrNull = (RyuSynchronizationContexts)await await ic.GetOrActivateAsyncExForModuleImport(typeof(RyuSynchronizationContexts), ActivationKind.ModuleRequire);
         } else {
            syncContextsOrNull = (RyuSynchronizationContexts)(await ic.TryGetAsync(typeof(RyuSynchronizationContexts))).Value;
         }

         var mainThreadTypes = new List<RyuType>();
         var nonMainThreadTypes = new List<RyuType>();
         foreach (var t in ryuTypesByType.Values) {
            if (!t.IsRequired() && !t.IsEventual()) continue;

            if (t.NeedsMainThread()) {
               if (EnableDebugLogging) Console.WriteLine($"Main Thread Type {t.Type.FullName}");
               mainThreadTypes.Add(t);
            } else {
               if (EnableDebugLogging) Console.WriteLine($"Background Thread Type {t.Type.FullName}");
               nonMainThreadTypes.Add(t);
            }
         }

         var mainThreadRequiredInstantiationTasks = new List<Task>();
         if (mainThreadTypes.Count > 0) {
            syncContextsOrNull.AssertIsNotNull($"The container declared a Main Thread Type but lacked a {typeof(RyuSynchronizationContexts)}");
            syncContextsOrNull.MainThread.AssertIsNotNull($"The container declared a Main Thread Type an has a {typeof(RyuSynchronizationContexts)}, but MainThread property is null");

            var mtsc = syncContextsOrNull.MainThread;
            mtsc.AssertIsActivated();

            foreach (var t in mainThreadTypes) {
               Assert.IsTrue(t.IsEventual() || t.IsRequired());

               if (EnableDebugLogging) Console.WriteLine($"MainThreadType {t.Type.FullName} required? {t.IsRequired()}");

               // This task completes when the on-main-thread activation has been queued (on the main sc)
               var queueTask = ic.GetOrActivateAsyncExForModuleImport(t.Type, ActivationKind.ModuleRequire);

               // This task completes when the on-main-thread activation has completed
               var instantiateTask = await queueTask;

               if (t.IsRequired()) {
                  mainThreadRequiredInstantiationTasks.Add(instantiateTask);
               } else {
                  instantiateTask.Forget();
               }
            }
         }

         var threadPoolSyncContext = syncContextsOrNull?.BackgroundThreadPool ?? DefaultThreadPoolSynchronizationContext.Instance;
         var nonMainThreadInstantiations = Task.WhenAll(nonMainThreadTypes.Map(t => Task.Run(async () => {
            await threadPoolSyncContext.YieldToAsync();
            await ic.GetOrActivateAsyncExForModuleImport(t.Type, ActivationKind.ModuleRequire);
         })));

         if (mainThreadRequiredInstantiationTasks.Count > 0) {
            syncContextsOrNull.MainThread.AssertIsActivated();

            // we're currently executing on the main synchronization context.
            await Task.WhenAll(mainThreadRequiredInstantiationTasks);
         }

         await nonMainThreadInstantiations;

         //container.ObjectActivated -= objectsByConstructionOrder.Enqueue;
      }

      /*
      public void InvokeInitializeDeprecated<T>(List<T> objs) {
         foreach (var obj in objs) {
            var type = obj.GetType();
            var initialize = type.GetTypeInfo().GetMethod("Initialize", BindingFlags.Public);
            if (initialize != null) {
               Console.WriteLine($">>>>>>>>> Invoking Initialize for {type.Name}");
               initialize.Invoke(obj, null);
            }
         }
      }
      */

      public class RyuExtensionArguments : IRyuExtensionArguments {
         public IRyuContainer Container { get; set; }
      }
   }
}