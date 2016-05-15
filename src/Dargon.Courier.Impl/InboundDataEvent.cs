﻿using System;
using System.Threading;
using Dargon.Commons.Pooling;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.Vox;
using NLog;

namespace Dargon.Courier {
   public class InboundDataEvent {
      public byte[] Data { get; set; }
   }

   public class InboundPayloadEvent {
      public InboundDataEvent DataEvent { get; set; }

      public object Payload { get; set; }
   }

   public class InboundPacketEvent {
      public InboundPayloadEvent PayloadEvent { get; set; }

      public PacketDto Packet => (PacketDto)PayloadEvent.Payload;
   }

   public interface InternalRoutableInboundMessageEvent {
      object Body { get; }
   }

   public interface IInboundMessageEvent<out T> {
      void AddRef();
      void ReleaseRef();

      InboundPacketEvent PacketEvent { get; }

      MessageDto Message { get; }
      T Body { get; }
      PeerContext Sender { get; }
   }

   public class InboundMessageEvent<T> :IInboundMessageEvent<T>, InternalRoutableInboundMessageEvent {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();
      private readonly IObjectPool<InboundMessageEvent<T>> eventPool;
      private int refCount = 0;

      public InboundMessageEvent(IObjectPool<InboundMessageEvent<T>> eventPool) {
         this.eventPool = eventPool;
      }

      ~InboundMessageEvent() {
         logger.Warn($"InboundMessageEvent of {typeof(T).FullName} leaked!");
         refCount = 0;
         eventPool.ReturnObject(this);
      }

      public void AddRef() => Interlocked.Increment(ref refCount);

      public void ReleaseRef() {
         if (Interlocked.Decrement(ref refCount) == 0) {
            PacketEvent = null;
            eventPool.ReturnObject(this);
         }
      }

      public InboundPacketEvent PacketEvent { get; set; }

      public MessageDto Message => (MessageDto) PacketEvent.Packet.Payload;
      public T Body => (T)Message.Body;
      public PeerContext Sender { get; set; }

      object InternalRoutableInboundMessageEvent.Body => Body;
   }

   public class OutboundPayloadEvent {
      public object Payload { get; set; }
      public object TagEvent { get; set; }
   }

   public class OutboundPacketEvent {
      public PacketDto Packet { get; set; }
      public object TagEvent { get; set; }
   }

   public class OutboundMessageEvent {
      public MessageDto Message { get; set; }
      public bool Reliable { get; set; }
      public object TagEvent { get; set; }
   }
}