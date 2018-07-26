using System.Net;
using Dargon.Courier.TransportTier.Tcp;

namespace Dargon.Courier.TransportTier.Udp {
   public static class CourierBuilderExtensions {
      public static CourierBuilder UseUdpTransport(this CourierBuilder builder) {
         return builder.UseTransport(new UdpTransportFactory());
      }

      public static CourierBuilder UseUdpTransport(this CourierBuilder builder, int unicastPort = -1) {
         var configurationBuilder = UdpTransportConfigurationBuilder.Create();
         if (unicastPort != -1) configurationBuilder.WithUnicastReceivePort(unicastPort);
         return builder.UseUdpTransport(configurationBuilder.Build());
      }

      public static CourierBuilder UseUdpTransport(this CourierBuilder builder, UdpTransportConfiguration configuration) {
         return builder.UseTransport(new UdpTransportFactory(configuration));
      }
   }
}
