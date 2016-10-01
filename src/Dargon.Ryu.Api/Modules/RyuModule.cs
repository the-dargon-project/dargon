using System;
using System.Collections.Generic;
using Dargon.Ryu.Internals;

namespace Dargon.Ryu.Modules {
   public delegate object RyuTypeActivator(IRyuContainer ryu);

   public interface IRyuModule {
      IReadOnlyDictionary<Type, RyuType> TypeInfoByType { get; }
   }

   public class RyuType {
      public Type Type { get; set; }
      public RyuTypeFlags Flags { get; set; }
      public RyuTypeActivator Activator { get; set; }
   }

   public class RyuModule : IRyuModule {
      private readonly Dictionary<Type, RyuType> typeInfoByType = new Dictionary<Type, RyuType>();
      public IReadOnlyDictionary<Type, RyuType> TypeInfoByType => typeInfoByType;

      public RyuRegisterOptions Register => new RyuRegisterOptions { Module = this };
      public RyuFluentOptions Optional => new RyuFluentOptions { Module = this, Flags = RyuTypeFlags.None };
      public RyuFluentOptions Required => new RyuFluentOptions { Module = this, Flags = RyuTypeFlags.Required };

      public void AddRyuType<T>(RyuType ryuType) => AddRyuType(typeof(T), ryuType);
      public void AddRyuType(Type type, RyuType ryuType) => typeInfoByType.Add(type, ryuType);
   }

   public class RyuRegisterOptions {
      public RyuModule Module { get; set; }
   }

   public class RyuFluentOptions {
      public RyuModule Module { get; set; }
      public RyuTypeFlags Flags { get; set; }

      public RyuFluentAdditions<T> Transient<T>(RyuTypeActivator activator = null) => Helper<T>(RyuTypeFlags.None, activator);
      public RyuFluentAdditions<T> Singleton<T>(RyuTypeActivator activator = null) => Helper<T>(RyuTypeFlags.Cache, activator);

      private RyuFluentAdditions<T> Helper<T>(RyuTypeFlags additionalFlags, RyuTypeActivator activator) {
         var ryuType = new RyuType {
            Type = typeof(T),
            Flags = Flags | additionalFlags,
            Activator = activator
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
         var isSingleton = (Flags & RyuTypeFlags.Cache) != 0;
         if (isSingleton) {
            this.Implements().Singleton(types);
         } else {
            this.Implements().Singleton(types);
         }
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
         public RyuFluentAdditions<T> Singleton(params Type[] types) => Helper(types, RyuTypeFlags.Cache, RyuTypeFlags.None);

         public RyuFluentAdditions<T> Transient<U1>() => Transient(typeof(U1));
         public RyuFluentAdditions<T> Transient<U1, U2>() => Transient(typeof(U1), typeof(U2));
         public RyuFluentAdditions<T> Transient<U1, U2, U3>() => Transient(typeof(U1), typeof(U2), typeof(U3));
         public RyuFluentAdditions<T> Transient(params Type[] types) => Helper(types, RyuTypeFlags.None, RyuTypeFlags.Cache);

         private RyuFluentAdditions<T> Helper(Type[] types, RyuTypeFlags addedFlags, RyuTypeFlags removedFlags) {
            var additions = FluentAdditions;
            foreach (var type in types) {
               additions.Module.AddRyuType(
                  type,
                  new RyuType {
                     Type = type,
                     Flags = (additions.Flags & ~removedFlags) | addedFlags,
                     Activator = (ryu) => ryu.GetOrActivate(additions.RyuType.Type)
                  });
            }
            return FluentAdditions;
         } 
      }
   }
}
