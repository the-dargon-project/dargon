using Dargon.Courier.AuditingTier;
using Dargon.Courier.ManagementTier;
using Dargon.Courier.ManagementTier.Vox;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.PubSubTier.Vox;
using Dargon.Courier.ServiceTier.Client;
using Dargon.Courier.ServiceTier.Vox;
using Dargon.Courier.StateReplicationTier.Vox;
using Dargon.Courier.TransportTier.Tcp.Vox;
//using Dargon.Courier.TransportTier.Udp.Vox;
using Dargon.Vox2;
using System.Collections.Generic;
using System;
using System.Reflection;
using Dargon.Commons;
using Dargon.Courier.StateReplicationTier.Primaries;
//using Dargon.Courier.Transports.Udp.Vox;

namespace Dargon.Courier.Vox {
   public enum CourierVoxTypeIds : int {
      // Courier Core (starts at 1 - note can't use 0 as that's TNull in Vox).
      [Type<MessageDto>] MessageDto = 1,

      // Peering
      PeeringBaseId = 5,
      [Type<WhoamiDto>] WhoamiDto = PeeringBaseId + 0,
      [Type<Identity>] Identity = PeeringBaseId + 1,

      // // Udp
      // UdpBaseId = 10,
      // [Type<PacketDto>] PacketDto = UdpBaseId + 0,
      // [Type<AcknowledgementDto>] AcknowledgementDto = UdpBaseId + 1,
      // [Type<AnnouncementDto>] AnnouncementDto = UdpBaseId + 2,
      // [Type<MultiPartChunkDto>] MultiPartChunkDto = UdpBaseId + 3,

      // Tcp
      TcpBaseId = 20,
      [Type<HandshakeDto>] HandshakeDto = TcpBaseId + 0,

      // Services
      ServiceBaseId = 30,
      [Type<RmiRequestDto>] RmiRequestDto = ServiceBaseId + 0,
      [Type<RmiResponseDto>] RmiResponseDto = ServiceBaseId + 1,
      [Type<RemoteException>] RemoteException = ServiceBaseId + 2,

      // Management
      ManagementBaseId = 40,
      [Type<ManagementObjectIdentifierDto>] ManagementObjectIdentifierDto = ManagementBaseId + 0,
      [Type<ManagementObjectStateDto>] ManagementObjectStateDto = ManagementBaseId + 1,
      [Type<MethodDescriptionDto>] MethodDescriptionDto = ManagementBaseId + 2,
      [Type<PropertyDescriptionDto>] PropertyDescriptionDto = ManagementBaseId + 3,
      [Type<DataSetDescriptionDto>] DataSetDescriptionDto = ManagementBaseId + 4,
      [Type<ParameterDescriptionDto>] ParameterDescriptionDto = ManagementBaseId + 5,
      [Type(typeof(ManagementDataSetDto<>))] ManagementDataSetDto = ManagementBaseId + 6,
      [Type(typeof(DataPoint<>))] DataPoint = ManagementBaseId + 7,
      [Type(typeof(AggregateStatistics<>))] AggregateStatistics = ManagementBaseId + 8,

      // PubSub
      PubSubBaseId = 50,
      [Type<PubSubNotification>] PubSubNotification = PubSubBaseId + 0,

      // StateTier
      StateTierBaseId = 60,
      [Type<ReplicationVersion>] ReplicationVersion = StateTierBaseId + 0,
      [Type<StateUpdateDto>] StateUpdateDto = StateTierBaseId + 1,

   }

   public class CourierVoxTypes : VoxAutoTypes<CourierVoxTypeIds> {
      public override List<Type> DependencyVoxTypes { get; } = new() {
         typeof(CoreVoxTypes),
      };
   };
}
