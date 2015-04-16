using System;
using System.IO;
using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;
using Dargon.Courier.Peering;
using Dargon.Courier.PortableObjects;
using Dargon.PortableObjects;
using ItzWarty;

namespace Dargon.Courier.Networking {
   public class NetworkReceiverImpl {
      private readonly ReadableCourierEndpoint localEndpoint;
      private readonly CourierNetworkContext networkContext;
      private readonly IPofSerializer pofSerializer;
      private readonly MessageRouter messageRouter;
      private readonly PeerRegistryImpl peerRegistry;

      public NetworkReceiverImpl(ReadableCourierEndpoint localEndpoint, CourierNetworkContext networkContext, IPofSerializer pofSerializer, MessageRouter messageRouter, PeerRegistryImpl peerRegistry) {
         this.localEndpoint = localEndpoint;
         this.networkContext = networkContext;
         this.pofSerializer = pofSerializer;
         this.messageRouter = messageRouter;
         this.peerRegistry = peerRegistry;
      }

      public void Initialize() {
         networkContext.DataArrived += HandleDataArrived;
      }

      private void HandleDataArrived(CourierNetwork network, byte[] data) {
         using (var ms = new MemoryStream(data))
         using (var reader = new BinaryReader(ms)) {
            ulong magic = reader.ReadUInt64();
            if (magic != NetworkingConstants.kMessageHeader) {
               return;
            }

            Guid senderId = reader.ReadGuid();
            var payload = pofSerializer.Deserialize(reader);
            if (payload is CourierMessageV1) {
               HandleInboundMessage(senderId, (CourierMessageV1)payload);
            } else if (payload is CourierAnnounceV1) {
               HandleInboundAnnounce(senderId, (CourierAnnounceV1)payload);
            }
         }
      }

      private void HandleInboundMessage(Guid senderId, CourierMessageV1 payload) {
//         Console.WriteLine(message.GetType());
         if (!localEndpoint.Matches(payload.RecipientId)) {
            return;
         }
         messageRouter.RouteMessage(senderId, payload);
      }

      private void HandleInboundAnnounce(Guid senderId, CourierAnnounceV1 payload) {
         peerRegistry.HandlePeerAnnounce(senderId, payload);
      }
   }
}
