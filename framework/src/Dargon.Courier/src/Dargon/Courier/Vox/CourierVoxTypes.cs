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
//using Dargon.Courier.Transports.Udp.Vox;

namespace Dargon.Courier.Vox {
   [AttributeUsage(AttributeTargets.Field)]
   public class TypeAttribute : Attribute {
      public TypeAttribute(Type dtoType, Type serializerType = null) {
         DtoType = dtoType;
         SerializerType = serializerType;
      }

      public Type DtoType { get; }
      public Type SerializerType { get; }
   }

   [AttributeUsage(AttributeTargets.Field)]
   public class TypeAttribute<TDtoType> : TypeAttribute {
      public TypeAttribute() : base(typeof(TDtoType), null) { }
   }

   [AttributeUsage(AttributeTargets.Field)]
   public class TypeAttribute<TDtoType, TSerializerType> : TypeAttribute {
      public TypeAttribute() : base(typeof(TDtoType), typeof(TSerializerType)) { }
   }

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
      [Type<StateUpdateDto>] StateUpdateDto = StateTierBaseId + 0,
   }

   public abstract class VoxAutoTypes<TTypeIds> : VoxTypes {
      private readonly List<Type> autoserializedTypes = new();
      private readonly Dictionary<Type, Type> typeToCustomSerializers = new();

      public VoxAutoTypes() {
         var tEnum = typeof(TTypeIds);
         tEnum.IsEnum.AssertIsTrue();

         var enumMembers = tEnum.GetMembers(BindingFlags.Public | BindingFlags.Static);
         foreach (var member in enumMembers) {
            foreach (var attr in member.GetCustomAttributes(true)) {
               if (attr is TypeAttribute ta) {
                  var dt = ta.DtoType;
                  if (ta.SerializerType is { } st) {
                     typeToCustomSerializers.Add(dt, st);
                  } else {
                     autoserializedTypes.Add(dt);
                  }
               }
            }
         }
      }

      public override List<Type> AutoserializedTypes => autoserializedTypes;
      public override Dictionary<Type, Type> TypeToCustomSerializers => typeToCustomSerializers;
   }

   public class CourierVoxTypes : VoxAutoTypes<CourierVoxTypeIds> {
      public override List<Type> DependencyVoxTypes { get; } = new() {
         typeof(CoreVoxTypes),
      };
   };
}
