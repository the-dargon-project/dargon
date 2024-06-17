using Dargon.Commons;
using Dargon.Ryu.Internals;
using Dargon.Ryu.Modules;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.AsyncAwait;
using Dargon.Commons.AsyncPrimitives;

namespace Dargon.Ryu {
   public class ConcurrentDictionaryWithOnceInvokedAddInvoke<K, V> {
      private static bool EnableDebugLog => false;

      private readonly ConcurrentDictionary<K, Entry> inner = new();

      public void Set(K key, V value) {
         if (!inner.TryGetValue(key, out var existing)) {
            int uniqueId = Interlocked.Increment(ref Entry.NextUniqueId);
            existing = inner.GetOrAdd(key, (_, x) => new Entry { UniqueId = x }, uniqueId);
            if (existing.UniqueId == uniqueId) {
               existing.ValueTcs.SetResult(value);
               return;
            }
         }

         var newTcs = new TaskCompletionSource<V>();
         newTcs.SetResult(value);
         existing.ValueTcs = newTcs;
      }

      public async Task<TryGetResult<V>> TryGetValueAsync(K key) {
         if (!inner.TryGetValue(key, out var entry)) {
            return (false, default(V));
         }

         return (true, await entry.ValueTcs.Task);
      }

      /// <summary>
      /// Returns a promise for the initialized instance's value.
      /// This method must return the promise synchronously.
      /// </summary>
      public Task<V> GetOrAddAsync(K key, Func<K, Task<V>> func) {
         if (!inner.TryGetValue(key, out var existing)) {
            int uniqueId = Interlocked.Increment(ref Entry.NextUniqueId);
            existing = inner.GetOrAdd(key, (_, x) => new Entry { UniqueId = x }, uniqueId);
            if (existing.UniqueId == uniqueId) {
               async Task<V> CallFactoryAsync() {
                  var keyStr = key.ToString();
                  if (EnableDebugLog) Console.WriteLine($"For Key {keyStr} run add func");
                  var t = func(key);

                  Task.Run(async () => {
                     await DefaultThreadPoolSynchronizationContext.Instance.YieldToAsync();
                     while (!t.IsCompleted) {
                        await Task.Delay(1000);
                        if (!t.IsCompleted) {
                           Console.WriteLine($"Warning: Instantiator task not yet completed for key {keyStr}");
                        }
                     }
                  }).Forget();

                  try {
                     var value = await t.ConfigureAwait(false);
                     if (EnableDebugLog) Console.WriteLine($"For Key {keyStr} completed add func");
                     existing.ValueTcs.SetResult(value);
                     return value;
                  } catch (Exception e) {
                     Console.WriteLine($"!!!!!!!!! For Key {keyStr} threw in add func {e}");
                     existing.ValueTcs.SetException(e);
                     throw;
                  }

               }

               return CallFactoryAsync();
            }
         }

         return existing.ValueTcs.Task;
      }

      // public V this[K key] {
      //    get => inner[key].Value.AssertNotEquals(default);
      //    set => inner[key] = new Entry(value) { UniqueId = -1 };
      // }

      private class Entry {
         public static int NextUniqueId;

         public required int UniqueId { get; init; }
         public TaskCompletionSource<V> ValueTcs { get; set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

         public Entry() { }

         public Entry(V value) => ValueTcs.SetResult(value);
      }
   }

   public class RyuContainer : IRyuContainer, IRyuContainerInternal, IRyuContainerForUserActivator {
      private readonly ConcurrentDictionaryWithOnceInvokedAddInvoke<Type, object> storage = new();
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

      public string Name { get; set; } = "Unnamed Container";
      internal IRyuContainerInternal I => this;
      IRyuContainer IRyuContainerInternal.__U => this;
      IRyuContainerForUserActivator IRyuContainerInternal.__A => this;
      public IRyuContainer AsUserRyuContainerUnsafe => this;

      public void Initialize() {
         storage.Set(typeof(IRyuContainer), this);
         storage.Set(typeof(RyuContainer), this);
      }

      public async Task<TryGetResult<object>> TryGetAsync(Type type) {
         var tgv = await storage.TryGetValueAsync(type);
         if (!tgv.Exists && parent != null) tgv = await parent.TryGetAsync(type);
         return tgv;
      }

      async Task<object> IRyuContainer.GetOrActivateAsync(Type type) => await await GetOrActivateAsyncExInternal(type, ActivationKind.UserActivate);

      async Task<object> IRyuContainerForUserActivator.GetOrActivateAsync(Type type) => await await GetOrActivateAsyncExInternal(type, ActivationKind.ExplicitActivatorDependency);

      async Task<object> IRyuContainerInternal.GetOrActivateAsync(Type type, ActivationKind activationKind) => await await GetOrActivateAsyncExInternal(type, ActivationKind.ExplicitActivatorDependency);

      Task<Task<object>> IRyuContainerInternal.GetOrActivateAsyncExForModuleImport(Type type, ActivationKind activationKind) => GetOrActivateAsyncExInternal(type, activationKind);

      public async Task<Task<object>> GetOrActivateAsyncExInternal(Type type, ActivationKind activationKind) {
         try {
            object result;
            if ((await storage.TryGetValueAsync(type)).Unpack(out result) ||
                (parent != null && (await parent.TryGetAsync(type)).Unpack(out result))) {
               return Task.FromResult(result);
            } else {
               AssertTypeCanBeActivated(type, activationKind);
               return storage.GetOrAddAsync(
                  type,
                  x => ActivateUntrackedAsync_InternalUnchecked(type, activationKind));
            }
         } catch (Exception e) {
            throw new RyuGetException(type, e);
         }
      }


      private void AssertTypeCanBeActivated(Type type, ActivationKind activationKind) {
         RyuType rt;
         if (ryuTypesByType.TryGetValue(type, out rt) ||
             (type.IsGenericType && ryuTypesByType.TryGetValue(type.GetGenericTypeDefinition(), out rt))) {
            if (rt.Flags.FastHasFlag(RyuTypeFlags.DenyDefaultActivate)) {
               Assert.IsTrue(rt.ActivatorAsync != null || rt.ActivatorSync != null);
            }
            // if ((rt.Flags & RyuTypeFlags.DenyUserActivate) != 0) {
            //    activationKind.AssertNotEquals(ActivationKind.UserActivate);
            // }
            // if ((rt.Flags & RyuTypeFlags.DenyDefaultActivatorDependency) != 0) {
            //    activationKind.AssertNotEquals(ActivationKind.DefaultActivatorDependency);
            // }
            // if ((rt.Flags & RyuTypeFlags.DenyModuleDependency) != 0) {
            //    activationKind.AssertNotEquals(ActivationKind.ModuleRequire);
            // }
            // if ((rt.Flags & RyuTypeFlags.DenyUserActivatorDependency) != 0) {
            //    activationKind.AssertNotEquals(ActivationKind.ExplicitActivatorDependency);
            // }
         }
      }

      Task<object> IRyuContainer.ActivateUntrackedAsync(Type type) => I.ActivateUntrackedAsync(type, ActivationKind.UserActivate);

      Task<object> IRyuContainerForUserActivator.ActivateUntrackedAsync(Type type) => I.ActivateUntrackedAsync(type, ActivationKind.ExplicitActivatorDependency);

      async Task<object> IRyuContainerInternal.ActivateUntrackedAsync(Type type, ActivationKind activationKind) {
         AssertTypeCanBeActivated(type, activationKind);
         return await ActivateUntrackedAsync_InternalUnchecked(type, activationKind);
      }

      private async Task<object> ActivateUntrackedAsync_InternalUnchecked(Type type, ActivationKind activationKind) {
         object result;
         RyuType ryuType;
         if (ryuTypesByType.TryGetValue(type, out ryuType)) {
            result = await activator.ActivateRyuTypeAsync(this, ryuType, activationKind);
         } else {
            result = await activator.ActivateDefaultTypeAsync(this, type, activationKind);
         }
         ObjectActivated?.Invoke(result);
         return result;
      }


      Task<object> IRyuContainer.ActivateUntrackedAsync(Type type, Dictionary<Type, object> additionalDependencies)
         => I.ActivateUntrackedAsync(type, additionalDependencies, ActivationKind.UserActivate);

      Task<object> IRyuContainerForUserActivator.ActivateUntrackedAsync(Type type, Dictionary<Type, object> additionalDependencies)
         => I.ActivateUntrackedAsync(type, additionalDependencies, ActivationKind.ExplicitActivatorDependency);

      async Task<object> IRyuContainerInternal.ActivateUntrackedAsync(Type type, Dictionary<Type, object> additionalDependencies, ActivationKind activationKind) {
         if (additionalDependencies.Count == 0) {
            return await I.ActivateUntrackedAsync(type, activationKind);
         }

         return activator.ActivateDefaultTypeAsync(
            new TransientActivationRyuContainer(this, additionalDependencies),
            type,
            ActivationKind.UserActivate);
      }

      Task<IEnumerable<object>> IRyuContainer.FindAsync(Type queryType) => I.FindAsync(queryType, ActivationKind.UserActivate);

      Task<IEnumerable<object>> IRyuContainerForUserActivator.FindAsync(Type queryType) => I.FindAsync(queryType, ActivationKind.ExplicitActivatorDependency);

      async Task<IEnumerable<object>> IRyuContainerInternal.FindAsync(Type queryType, ActivationKind activationKind) {
         var result = Enumerable.Empty<object>();

         if (parent != null) {
            result = result.Concat(parent.FindAsync(queryType));
         }

         HashSet<RyuType> implementors;
         if (implementorsByType.TryGetValue(queryType, out implementors)) {
            foreach (var x in implementors) {
               await I.GetOrActivateAsync(x.Type, activationKind);
            }
         }

         HashSet<object> instances;
         if (instancesImplementingType.TryGetValue(queryType, out instances)) {
            result = result.Concat(instances);
         }

         return result;
      }

      public void Set(Type type, object instance) {
         storage.Set(type, instance);
      }

      public IRyuContainer CreateChildContainer(string name) {
         var c = new RyuContainer(this, activator, ryuTypesByType);
         c.Name = name;
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
            ryuTypesByType.AddOrUpdate(
               kvp.Key,
               (_, x) => x,
               (_, existing, addition) => existing.Merge(addition),
               kvp.Value);
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

   /// <summary>
   /// A temporary Ryu container used for injecting arguments
   /// when activating a transient instance.
   /// </summary>
   internal class TransientActivationRyuContainer(IRyuContainer parent, Dictionary<Type, object> storage) : IRyuContainer, IRyuContainerInternal {
      public string Name {
         get => nameof(TransientActivationRyuContainer);
         set => throw new NotImplementedException();
      }

      public IRyuContainer __U => this;
      public IRyuContainerForUserActivator __A => throw new NotSupportedException();

      public async Task<TryGetResult<object>> TryGetAsync(Type type) {
         if (storage.TryGetValue(type, out var value)) {
            return (true, value);
         } else {
            return await parent.TryGetAsync(type);
         }
      }

      public Task<object> GetOrActivateAsync(Type type) => GetOrActivateAsync(type, ActivationKind.UserActivate);

      public async Task<object> GetOrActivateAsync(Type type, ActivationKind activationKind) {
         activationKind.AssertEquals(ActivationKind.DefaultActivatorDependency);
         return storage.TryGetValue(type, out var value)
            ? value
            : await parent.GetOrActivateAsync(type);
      }


      public Task<Task<object>> GetOrActivateAsyncExForModuleImport(Type type, ActivationKind activationKind) => throw new NotSupportedException();

      public Task<object> ActivateUntrackedAsync(Type type) => throw new NotSupportedException();
      public Task<object> ActivateUntrackedAsync(Type type, ActivationKind activationKind) => throw new NotSupportedException();
      public object ActivateUntracked(Type type) => throw new NotSupportedException();
      public Task<object> ActivateUntrackedAsync(Type type, Dictionary<Type, object> additionalDependencies) => throw new NotSupportedException();
      public Task<object> ActivateUntrackedAsync(Type type, Dictionary<Type, object> additionalDependencies, ActivationKind activationKind) => throw new NotSupportedException();
      public object ActivateUntracked(Type type, Dictionary<Type, object> additionalDependencies) => throw new NotSupportedException();
      public Task<IEnumerable<object>> FindAsync(Type queryType) => throw new NotSupportedException();
      public Task<IEnumerable<object>> FindAsync(Type queryType, ActivationKind activationKind) => throw new NotSupportedException();
      public IEnumerable<object> Find(Type queryType) => throw new NotSupportedException();
      public void Set(Type type, object instance) => throw new NotSupportedException();
      public IRyuContainer CreateChildContainer(string name) => throw new NotSupportedException();
   }
}
