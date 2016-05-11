using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Dargon.Commons.Collections;
using Dargon.Commons.Utilities;
using Dargon.Courier.PeeringTier;

namespace Dargon.Courier.ServiceTier.Client {
   public class RemoteServiceProxyContainer {
      private readonly ConcurrentDictionary<Type, object> servicesByType = new ConcurrentDictionary<Type, object>();
      private readonly ProxyGenerator proxyGenerator;
      private readonly RemoteServiceInvoker remoteServiceInvoker;

      public RemoteServiceProxyContainer(ProxyGenerator proxyGenerator, RemoteServiceInvoker remoteServiceInvoker) {
         this.proxyGenerator = proxyGenerator;
         this.remoteServiceInvoker = remoteServiceInvoker;
      }

      public T Get<T>(PeerContext peer) {
         Guid serviceId;
         if (!typeof(T).TryGetInterfaceGuid(out serviceId)) {
            throw new InvalidOperationException($"Service of type {typeof(T).FullName} does not have default service id.");
         }
         return Get<T>(serviceId, peer);
      }

      public T Get<T>(Guid serviceId, PeerContext peer) {
         return (T)servicesByType.GetOrAdd(
            typeof(T),
            add => CreateInvocationProxy<T>(serviceId, peer));
      }

      private T CreateInvocationProxy<T>(Guid serviceId, PeerContext peer) {
         var remoteServiceInfo = new RemoveServiceInfo {
            Peer =  peer,
            ServiceType = typeof(T),
            ServiceId = serviceId
         };
         var interceptor = new RemoteServiceProxyInvocationInterceptor(remoteServiceInfo, remoteServiceInvoker);
         return (T)proxyGenerator.CreateInterfaceProxyWithoutTarget(typeof(T), interceptor);
      }
   }
}
