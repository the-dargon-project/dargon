using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.Identities;
using ItzWarty;
using ItzWarty.Collections;

namespace Dargon.Courier.Networking {
   public class LocalCourierNetwork : CourierNetwork {
      private static readonly IPEndPoint localEndpoint = new IPEndPoint(IPAddress.Loopback, 0);
      private readonly IConcurrentSet<NetworkContextImpl> contexts = new ConcurrentSet<NetworkContextImpl>();
      private readonly double dropRate;

      public LocalCourierNetwork(double dropRate) {
         this.dropRate = dropRate;
      }

      public IPEndPoint LocalEndpoint => localEndpoint;

      public CourierNetworkContext Join(ReadableCourierEndpoint endpoint) {
         var context = new NetworkContextImpl(this);
         contexts.Add(context);
         return context;
      }

      private void Broadcast(NetworkContextImpl senderContext, byte[] buffer, int offset, int length) {
         byte[] payload = new byte[length];
         Buffer.BlockCopy(buffer, offset, payload, 0, length);

         if (StaticRandom.NextDouble() > Math.Sqrt(1 - dropRate)) {
            return;
         }
         foreach (var context in contexts) {
            if (StaticRandom.NextDouble() > Math.Sqrt(1 - dropRate)) {
               return;
            }
            if (context != senderContext) {
               context.HandleDataArrived(payload);
            }
         }
      }

      private class NetworkContextImpl : CourierNetworkContext {
         private readonly LocalCourierNetwork network;

         public NetworkContextImpl(LocalCourierNetwork network) {
            this.network = network;
         }

         public void Broadcast(byte[] payload) {
            Broadcast(payload, 0, payload.Length);
         }

         public void Broadcast(byte[] payload, int offset, int length) {
            network.Broadcast(this, payload, offset, length);
         }

         public void HandleDataArrived(byte[] data) {
            DataArrived?.BeginInvoke(network, data, 0, data.Length, network.LocalEndpoint, null, null);
         }

         public event DataArrivedHandler DataArrived;
      }
   }
}
