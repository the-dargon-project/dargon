using System;
using Dargon.Courier.ServiceTier.Client;
using Dargon.Courier.ServiceTier.Vox;
using Dargon.Vox;

namespace Dargon.Courier.Vox {
   public class CourierVoxTypes : VoxTypes {
      public CourierVoxTypes() : base(0) {
         // Courier Core
         Register<PacketDto>(0);
         Register<MessageDto>(1);
         Register<AcknowledgementDto>(2);
         Register<AnnouncementDto>(3);
         Register<Identity>(4);

         // Services
         var serviceBaseId = 10;
         Register<RmiRequestDto>(serviceBaseId + 0);
         Register<RmiResponseDto>(serviceBaseId + 1);
         Register<RemoteException>(serviceBaseId + 2);
      }
   }
}
