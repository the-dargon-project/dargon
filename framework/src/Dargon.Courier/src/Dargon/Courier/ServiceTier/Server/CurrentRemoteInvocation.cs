using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.ServiceTier.Vox;

namespace Dargon.Courier.ServiceTier.Server {
   /// <summary>
   /// Globals intended for consumption by applications, not courier / library state.
   /// </summary>
   public static class CourierGlobals {
      public static readonly AsyncLocal<IInboundMessageEvent> AlsCurrentInboundMessageEventStore = new AsyncLocal<IInboundMessageEvent>();
      public static IInboundMessageEvent AlsCurrentInboundMessageEvent => AlsCurrentInboundMessageEventStore.Value.AssertIsNotNull("sAlsCurrentInboundMessageEvent is null!");
      public static PeerContext AlsCurrentInboundMessagePeer => AlsCurrentInboundMessageEvent.Sender;
   }
}
