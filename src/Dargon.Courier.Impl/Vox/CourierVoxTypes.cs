using Dargon.Courier.ManagementTier;
using Dargon.Courier.ServiceTier.Client;
using Dargon.Courier.ServiceTier.Vox;
using Dargon.Courier.TransportTier.Tcp.Vox;
using Dargon.Courier.Vox;
using Dargon.Vox;

namespace Dargon.Courier.TransportTier.Udp.Vox {
   public class CourierVoxTypes : VoxTypes {
      public CourierVoxTypes() : base(0) {
         // Courier Core
         Register<MessageDto>(0);

         // Udp
         var udpBaseId = 10;
         Register<PacketDto>(udpBaseId + 0);
         Register<AcknowledgementDto>(udpBaseId + 1);
         Register<AnnouncementDto>(udpBaseId + 2);
         Register<Identity>(udpBaseId + 3);

         // Tcp
         var tcpBaseId = 20;
         Register<HandshakeDto>(tcpBaseId + 0);

         // Services
         var serviceBaseId = 30;
         Register<RmiRequestDto>(serviceBaseId + 0);
         Register<RmiResponseDto>(serviceBaseId + 1);
         Register<RemoteException>(serviceBaseId + 2);

         // Management
         var managementBaseId = 40;
         Register<ManagementObjectIdentifierDto>(managementBaseId + 0);
         Register<ManagementObjectStateDto>(managementBaseId + 1);
         Register<MethodDescriptionDto>(managementBaseId + 2);
         Register<ParameterDescriptionDto>(managementBaseId + 3);
      }
   }
}
