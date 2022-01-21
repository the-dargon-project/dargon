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

   public interface IInboundMessageEvent {
      MessageDto Message { get; }
      PeerContext Sender { get; }
      Guid SenderId => Message.SenderId;
   }

   public interface IInboundMessageEvent<out T> : IInboundMessageEvent {
      T Body { get; }
   }

   public static class InboundMessageEventExtensions {
      public static IInboundMessageEvent<TBody> CastWithMessageBodyType<TBody>(this IInboundMessageEvent e) {
         return (IInboundMessageEvent<TBody>)e;
      }
   }

   /// <summary>
   /// Do not hold onto references of <seealso cref="InboundMessageEvent{T}"/>! They are pooled/reused
   /// and only valid for the duration of a message route / message handler / remote invocation.
   /// </summary>
   /// <typeparam name="T"></typeparam>
   public class InboundMessageEvent<T> : IInboundMessageEvent<T>, InternalRoutableInboundMessageEvent {
      public MessageDto Message { get; set; }
      public T Body => (T)Message.Body;
      public PeerContext Sender { get; set; }

      object InternalRoutableInboundMessageEvent.Body => Body;

      public override string ToString() => $"[IME {Body}]";
   }
   
   public class OutboundMessageEvent {
      public MessageDto Message { get; set; }
      public bool Reliable { get; set; }
      public object TagEvent { get; set; }
   }
}