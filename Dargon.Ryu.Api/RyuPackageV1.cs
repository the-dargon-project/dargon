using System;
using System.Collections.Generic;
using System.Net.Configuration;

namespace Dargon.Ryu {
   public class RyuPackageV1TypeInfo {
      public Type Type { get; set; }
      public Func<RyuContainer, object> GetInstance { get; set; } 
      public RyuTypeFlags Flags { get; set; }
   }

   public class RyuPackageV1 {
      private readonly Dictionary<Type, RyuPackageV1TypeInfo> typeInfoByType = new Dictionary<Type, RyuPackageV1TypeInfo>();
      private readonly ISet<Type> remoteServiceTypes = new HashSet<Type>();

      public IReadOnlyDictionary<Type, RyuPackageV1TypeInfo> TypeInfoByType => typeInfoByType;
      public ISet<Type> RemoteServiceTypes => remoteServiceTypes;

      public void Register(Type type, Func<RyuContainer, object> getInstance, RyuTypeFlags flags = RyuTypeFlags.None) {
         typeInfoByType.Add(
            type,
            new RyuPackageV1TypeInfo {
               Type = type,
               GetInstance = getInstance,
               Flags = flags
            }
         );
      }

      public void Instance(Type type, Func<RyuContainer, object> getInstance, RyuTypeFlags additionalFlags = RyuTypeFlags.None) {
         Register(
            type,
            getInstance,
            additionalFlags
         );
      }

      public void Instance<Type>(Func<RyuContainer, object> getInstance, RyuTypeFlags additionalFlags = RyuTypeFlags.None) {
         Instance(typeof(Type), getInstance, additionalFlags);
      }

      public void Instance(Type interfaceType, Type implementationType, RyuTypeFlags additionalFlags = RyuTypeFlags.None) {
         Instance(interfaceType, ryu => ryu.Get(implementationType), additionalFlags);
      }

      public void Instance<Interface>(Type implementationType, RyuTypeFlags additionalFlags = RyuTypeFlags.None) {
         Instance<Interface>(ryu => ryu.Get(implementationType), additionalFlags);
      }

      public void Instance<Interface, Implementation>(RyuTypeFlags additionalFlags = RyuTypeFlags.None) {
         Instance<Interface>(typeof(Implementation), additionalFlags);
      }

      public void Instance<Type>(RyuTypeFlags additionalFlags = RyuTypeFlags.None) {
         Instance<Type>(ryu => ryu.Construct<Type>(), additionalFlags);
      }

      public void Singleton(Type type, Func<RyuContainer, object> getInstance, RyuTypeFlags additionalFlags = RyuTypeFlags.None) {
         Register(
            type,
            getInstance,
            additionalFlags | RyuTypeFlags.Cache
         );
      }

      public void Singleton<Type>(Func<RyuContainer, object> getInstance, RyuTypeFlags additionalFlags = RyuTypeFlags.None) {
         Singleton(typeof(Type), getInstance, additionalFlags);
      }

      public void Singleton(Type interfaceType, Type implementationType, RyuTypeFlags additionalFlags = RyuTypeFlags.None) {
         Singleton(interfaceType, ryu => ryu.Get(implementationType), additionalFlags);
      }

      public void Singleton<Interface>(Type implementationType, RyuTypeFlags additionalFlags = RyuTypeFlags.None) {
         Singleton<Interface>(ryu => ryu.Get(implementationType), additionalFlags);
      }

      public void Singleton<Interface, Implementation>(RyuTypeFlags additionalFlags = RyuTypeFlags.None) {
         Singleton<Interface>(typeof(Implementation), additionalFlags);
      }

      public void Singleton<Type>(RyuTypeFlags additionalFlags = RyuTypeFlags.None) {
         Singleton<Type>(ryu => ryu.ForceConstruct<Type>(), additionalFlags);
      }

      public void RemoteService<Interface>() {
         remoteServiceTypes.Add(typeof(Interface));
      }

      public void LocalService<Implementation>(Type serviceInterface, RyuTypeFlags additionalFlags, bool required = true) {
         RyuTypeFlags flags = RyuTypeFlags.Service | additionalFlags;
         if (required) {
            flags |= RyuTypeFlags.Required;
         }
         Singleton(serviceInterface, typeof(Implementation), flags);
      }

      public void LocalService<Interface, Implementation>(RyuTypeFlags additionalFlags, bool required = true) {
         LocalService<Implementation>(typeof(Interface), additionalFlags, required: required);
      }

      public void LocalService<Implementation>(Type[] serviceInterfaces, RyuTypeFlags additionalFlags, bool required = true) {
         foreach (var serviceInterface in serviceInterfaces) {
            LocalService<Implementation>(serviceInterface, additionalFlags, required: required);
         }
      }

      public void Mob<T>(RyuTypeFlags additionalFlags, bool required = true) {
         RyuTypeFlags flags = RyuTypeFlags.ManagementObject | additionalFlags;
         if (required) {
            flags |= RyuTypeFlags.Required;
         }
         Singleton<T>(flags);
      }

      public void PofContext<T>() {
         Singleton<T>(RyuTypeFlags.PofContext);
      }
   }
}