using System;
using System.Threading;
using Dargon.Commons.Pooling;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.Vox;
using NLog;

namespace Dargon.Courier {
   public interface InternalRoutableInboundMessageEvent {
      object Body { get; }
   }

   public interface IInboundMessageEvent<out T> {
      MessageDto Message { get; }
      T Body { get; }
      PeerContext Sender { get; }
      Guid SenderId { get; }
   }

   public class InboundMessageEvent<T> : IInboundMessageEvent<T>, InternalRoutableInboundMessageEvent {
      public MessageDto Message { get; set; }
      public T Body => (T)Message.Body;
      public PeerContext Sender { get; set; }
      public Guid SenderId => Sender.Identity.Id;

      object InternalRoutableInboundMessageEvent.Body => Body;
   }
   
   public class OutboundMessageEvent {
      public MessageDto Message { get; set; }
      public bool Reliable { get; set; }
      public object TagEvent { get; set; }
   }
}