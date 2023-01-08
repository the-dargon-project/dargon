using Dargon.Commons;
using Dargon.Ryu.Internals;
using Dargon.Ryu.Modules;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dargon.Ryu {
   public class RyuContainer : IRyuContainer {
      private readonly ConcurrentDictionary<Type, object> storage = new ConcurrentDictionary<Type, object>();
      private readonly IRyuContainer parent;
      private readonly IActivator activator;
      private readonly ConcurrentDictionary<Type, RyuType> ryuTypesByType;
      private readonly ConcurrentDictionary<Type, HashSet<RyuType>> implementorsByType = new ConcurrentDictionary<Type, HashSet<RyuType>>();
      private readonly ConcurrentDictionary<Type, HashSet<object>> instancesImplementingType = new ConcurrentDictionary<Type, HashSet<object>>();

      public event Action<object> ObjectActivated;

      public RyuContainer(
         IRyuContainer parent, 
         IActivator activator
      ) : this(parent, activator, new ConcurrentDictionary<Type, RyuType>()) {
      }

      public RyuContainer(IRyuContainer parent, IActivator activator, ConcurrentDictionary<Type, RyuType> ryuTypesByType) {
         this.parent = parent;
         this.activator = activator;
         this.ryuTypesByType = ryuTypesByType;
      }

      public void Initialize() {
         storage[typeof(IRyuContainer)] = this;
         storage[typeof(RyuContainer)] = this;
      }

      public bool TryGet(Type type, out object value) {
         return storage.TryGetValue(type, out value) ||
                (parent != null && parent.TryGet(type, out value));
      }

      public object GetOrActivate(Type type) {
         try {
            object result;
            if (storage.TryGetValue(type, out result) ||
                (parent != null && parent.TryGet(type, out result))) {
               return result;
            } else {
               return storage.GetOrAdd(
                  type,
                  x => ActivateUntracked(type));
            }
         } catch (Exception e) {
            throw new RyuGetException(type, e);
         }
      }

      public object ActivateUntracked(Type type) {
         object result;
         RyuType ryuType;
         if (ryuTypesByType.TryGetValue(type, out ryuType)) {
            result = activator.ActivateRyuType(this, ryuType);
         } else if (type.GetTypeInfo().IsAbstract || type.GetTypeInfo().IsInterface) {
            throw new ImplementationNotFoundException(type);
         } else {
            result = activator.ActivateDefaultType(this, type);
         }
         ObjectActivated?.Invoke(result);
         return result;
      }

      public IEnumerable<object> Find(Type queryType) {
         var result = Enumerable.Empty<object>();

         if (parent != null) {
            result = result.Concat(parent.Find(queryType));
         }

         HashSet<RyuType> implementors;
         if (implementorsByType.TryGetValue(queryType, out implementors)) {
            implementors.ForEach(x => GetOrActivate(x.Type));
         }

         HashSet<object> instances;
         if (instancesImplementingType.TryGetValue(queryType, out instances)) {
            result = result.Concat(instances);
         }

         return result;
      }

      public void Set(Type type, object instance) {
         storage[type] = instance;
      }

      public IRyuContainer CreateChildContainer() {
         var c = new RyuContainer(this, activator, ryuTypesByType);
         c.Initialize();
         if (this.TryGet(out IRyuFacade facade)) {
            var childFacade = new RyuFacade(c, activator, facade.ModuleImporter);
            c.Set<RyuFacade>(childFacade);
            c.Set<IRyuFacade>(childFacade);
         }
         return c;
      }

      public void Import(Dictionary<Type, RyuType> importedRyuTypesByType, ConcurrentDictionary<Type, HashSet<RyuType>> importedTypeToImplementors) {
         foreach (var kvp in importedRyuTypesByType) {
            ryuTypesByType[kvp.Key] = kvp.Value;
         }
         foreach (var kvp in importedTypeToImplementors) {
            implementorsByType.AddOrUpdate(
               kvp.Key,
               add => kvp.Value,
               (update, existing) => existing.Tap(
                  x => kvp.Value.ForEach(v => existing.Add(v))
                  ));
         }
      }
   }
}
