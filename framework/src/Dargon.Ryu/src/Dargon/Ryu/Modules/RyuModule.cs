using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dargon.Commons;

namespace Dargon.Ryu.Modules {
   public delegate object RyuTypeActivatorSync(IRyuContainerForUserActivator ryu);
   public delegate Task<object> RyuTypeActivatorAsync(IRyuContainerForUserActivator ryu);

   public interface IRyuModule {
      string Name { get; }
      IReadOnlyDictionary<Type, RyuType> TypeInfoByType { get; }
      RyuModuleFlags Flags { get; }
   }

   [Flags]
   public enum RyuModuleFlags {
      Default = 0,
      AlwaysLoad
   }

   public class RyuType {
      public Type Type { get; set; }
      public RyuTypeFlags Flags { get; set; }
      public RyuTypeActivatorSync ActivatorSync { get; set; }
      public RyuTypeActivatorAsync ActivatorAsync { get; set; }
      public required object DeclaredBy { get; set; }

      public event Action<object> OnActivated;

      internal void HandleOnActivated(object inst) => OnActivated?.Invoke(inst);

      internal RyuType Merge(RyuType other) {
         Assert.IsTrue(
            (ActivatorSync != null || ActivatorAsync != null) ^
            (other.ActivatorSync != null || other.ActivatorAsync != null));

         Type.AssertEquals(other.Type);
         Flags |= other.Flags;
         ActivatorSync ??= other.ActivatorSync;
         ActivatorAsync ??= other.ActivatorAsync;
         DeclaredBy = new[] { DeclaredBy, other.DeclaredBy };
         return this;
      }
   }

   public abstract class RyuModule : IRyuModule {
      public virtual string Name => GetType().FullName;
      private readonly Dictionary<Type, RyuType> typeInfoByType = new Dictionary<Type, RyuType>();
      public IReadOnlyDictionary<Type, RyuType> TypeInfoByType => typeInfoByType;
      public virtual RyuModuleFlags Flags => RyuModuleFlags.Default;

      public RyuFluentOptions Optional => new RyuFluentOptions { Module = this, Flags = RyuTypeFlags.None };
      public RyuFluentOptions Eventual => new RyuFluentOptions { Module = this, Flags = RyuTypeFlags.Eventual };
      public RyuFluentOptions Required => new RyuFluentOptions { Module = this, Flags = RyuTypeFlags.Required };

      public void AddRyuType<T>(RyuType ryuType) => AddRyuType(typeof(T), ryuType);
      public void AddRyuType(Type type, RyuType ryuType) {
         typeInfoByType.Add(type, ryuType);
         OnTypeAdded(ryuType);
      }

      protected virtual void OnTypeAdded(RyuType ryuType) {}
   }

   public class LambdaRyuModule : RyuModule {
      public LambdaRyuModule(Action<LambdaRyuModule> init, RyuModuleFlags flags = RyuModuleFlags.Default) {
         Flags = flags;
         init(this);
      }

      public override RyuModuleFlags Flags { get; }

      public static LambdaRyuModule ForRequired<T>(Func<IRyuContainerForUserActivator, T> activator, RyuModuleFlags flags = RyuModuleFlags.AlwaysLoad) {
         return new LambdaRyuModule(m => m.Required.Singleton<T>(activator), flags);
      }
   }

   public class RyuRegisterOptions {
      public RyuModule Module { get; set; }
   }

   public class RyuFluentOptions {
      internal RyuModule Module { get; set; }
      internal RyuTypeFlags Flags { get; set; }

      // RyuTypeActivator activator = null used to be an arg to Transient/Singleton, not sure why
      // in any case, it's a bad idea as that allows type mismatches as activator returns an object but
      // the user has specified a type T...
      public RyuFluentAdditions<T> Transient<T>() => Helper<T>(RyuTypeFlags.None, null, null);
      public RyuFluentAdditions<T> Transient<T>(Func<IRyuContainerForUserActivator, T> activator) => Helper<T>(RyuTypeFlags.None, ryu => activator(ryu), null);
      public RyuFluentAdditions<T> Transient<T>(Func<IRyuContainerForUserActivator, Task<T>> activator) => Helper<T>(RyuTypeFlags.None, null, async ryu => await activator(ryu));
      public RyuFluentAdditions<T> Singleton<T>() => Helper<T>(RyuTypeFlags.Singleton, null, null);
      public RyuFluentAdditions<T> Singleton<T>(Func<IRyuContainerForUserActivator, T> activator) => Helper<T>(RyuTypeFlags.Singleton, ryu => activator(ryu), null);
      public RyuFluentAdditions<T> Singleton<T>(Func<IRyuContainerForUserActivator, Task<T>> activator) => Helper<T>(RyuTypeFlags.Singleton, null, async ryu => await activator(ryu));

      private RyuFluentAdditions<T> Helper<T>(RyuTypeFlags additionalFlags, RyuTypeActivatorSync activatorSync, RyuTypeActivatorAsync activatorAsync) {
         var ryuType = new RyuType {
            Type = typeof(T),
            Flags = Flags | additionalFlags,
            ActivatorSync = activatorSync,
            ActivatorAsync = activatorAsync,
            DeclaredBy = Module, 
         };
         Module.AddRyuType<T>(ryuType);
         return new RyuFluentAdditions<T> { Module = Module, Flags = Flags, RyuType = ryuType };
      }
   }

   public class RyuFluentAdditions<T> {
      public RyuModule Module { get; set; }
      public RyuTypeFlags Flags { get; set; }
      public RyuType RyuType { get; set; }

      public RyuFluentAdditions<T> Implements<U1>() => ImplementsHelper(typeof(U1));
      public RyuFluentAdditions<T> Implements<U1, U2>() => ImplementsHelper(typeof(U1), typeof(U2));
      public RyuFluentAdditions<T> Implements<U1, U2, U3>() => ImplementsHelper(typeof(U1), typeof(U2), typeof(U3));

      private RyuFluentAdditions<T> ImplementsHelper(params Type[] types) {
         var isSingleton = (Flags & RyuTypeFlags.Singleton) != 0;
         if (isSingleton) {
            this.Implements().Singleton(types);
         } else {
            this.Implements().Transient(types);
         }
         return this;
      }

      public RyuFluentAdditions<T> RequiresMainThread() {
         RyuType.Flags |= RyuTypeFlags.RequiresMainThread;
         return this;
      }

      public RyuFluentAdditions<T> DenyDefaultActivate() {
         RyuType.Flags |= RyuTypeFlags.DenyDefaultActivate;
         return this;
      }
   }

   public static class RyuFluentAdditionsImplements {
      public static RyuFluentAdditionsImplementsFluent<T> Implements<T>(this RyuFluentAdditions<T> self) {
         return new RyuFluentAdditionsImplementsFluent<T> { FluentAdditions = self };
      }

      public class RyuFluentAdditionsImplementsFluent<T> {
         public RyuFluentAdditions<T> FluentAdditions { get; set; }

         public RyuFluentAdditions<T> Singleton<U1>() => Singleton(typeof(U1));
         public RyuFluentAdditions<T> Singleton<U1, U2>() => Singleton(typeof(U1), typeof(U2));
         public RyuFluentAdditions<T> Singleton<U1, U2, U3>() => Singleton(typeof(U1), typeof(U2), typeof(U3));
         public RyuFluentAdditions<T> Singleton(params Type[] types) => Helper(types, RyuTypeFlags.Singleton, RyuTypeFlags.None);

         public RyuFluentAdditions<T> Transient<U1>() => Transient(typeof(U1));
         public RyuFluentAdditions<T> Transient<U1, U2>() => Transient(typeof(U1), typeof(U2));
         public RyuFluentAdditions<T> Transient<U1, U2, U3>() => Transient(typeof(U1), typeof(U2), typeof(U3));
         public RyuFluentAdditions<T> Transient(params Type[] types) => Helper(types, RyuTypeFlags.None, RyuTypeFlags.Singleton);

         private RyuFluentAdditions<T> Helper(Type[] types, RyuTypeFlags addedFlags, RyuTypeFlags removedFlags) {
            var additions = FluentAdditions;
            foreach (var type in types) {
               additions.Module.AddRyuType(
                  type,
                  new RyuType {
                     Type = type,
                     Flags = (additions.Flags & ~removedFlags) | addedFlags,
                     ActivatorAsync = (ryu) => ryu.GetOrActivateAsync(additions.RyuType.Type),
                     DeclaredBy = FluentAdditions.Module,
                  });
            }
            return FluentAdditions;
         } 
      }
   }
}
