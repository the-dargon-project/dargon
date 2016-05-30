using System.Net;
using Dargon.Courier.TransportTier.Tcp;

namespace Dargon.Courier.TransportTier.Udp {
   public static class CourierBuilderExtensions {
      public static CourierBuilder UseUdpMulticastTransport(this CourierBuilder builder) {
         return builder.UseTransport(new UdpTransportFactory());
      }

      public static CourierBuilder UseUdpMulticastTransport(this CourierBuilder builder, UdpTransportConfiguration configuration) {
         return builder.UseTransport(new UdpTransportFactory(configuration));
      }
   }
}
