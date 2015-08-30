using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Services;
using SCG = System.Collections.Generic;

namespace Dargon.Ryu {
   public class RyuActivator {
      private readonly SCG.IDictionary<Type, RyuPackageV1TypeInfo> typeInfosByType;
      private readonly RyuContainer container;

      public object Activate(Type type) {
         RyuPackageV1TypeInfo typeInfo;
         if (typeInfosByType.TryGetValue(type, out typeInfo)) {
            if (typeInfo.Flags.HasFlag(RyuTypeFlags.Cache)) {
               return instancesByType.GetOrAdd(type, add => typeInfo.GetInstance(this));
            } else {
               return typeInfo.GetInstance(this);
            }
         } else if (type.IsInterface) {
            if (remoteServices.Contains(type)) {
               var serviceClient = Get<IServiceClient>();
               return typeof(IServiceClient).GetMethod(nameof(serviceClient.GetService)).MakeGenericMethod(type).Invoke(serviceClient, null);
            } else if (!LoadAdditionalAssemblies()) {
               throw new ImplementationNotDefinedException(type);
            } else {
               return GetUninstantiated(type);
            }
         } else {
            return ConstructAndInitialize(GetRyuConstructorOrThrow(type));
         }

      }
   }
}
