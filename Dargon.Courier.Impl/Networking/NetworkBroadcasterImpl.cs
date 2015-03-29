using System;
using System.IO;
using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;
using Dargon.PortableObjects;
using ItzWarty;

namespace Dargon.Courier.Networking {
   public class NetworkBroadcasterImpl : NetworkBroadcaster {
      private readonly ReadableCourierEndpoint localEndpoint;
      private readonly CourierNetworkContext networkContext;
      private readonly PofSerializer pofSerializer;

      public NetworkBroadcasterImpl(ReadableCourierEndpoint localEndpoint, CourierNetworkContext networkContext, PofSerializer pofSerializer) {
         this.localEndpoint = localEndpoint;
         this.networkContext = networkContext;
         this.pofSerializer = pofSerializer;
      }

      public void SendCourierPacket<TPayload>(TPayload payload) {
         using (var ms = new MemoryStream())
         using (var writer = new BinaryWriter(ms)) {
            writer.Write((ulong)NetworkingConstants.kMessageHeader);
            writer.Write((Guid)localEndpoint.Identifier);
            pofSerializer.Serialize(writer, (object)payload);

            networkContext.Broadcast(ms.GetBuffer(), 0, (int)ms.Length);
         }
      }
   }
}
