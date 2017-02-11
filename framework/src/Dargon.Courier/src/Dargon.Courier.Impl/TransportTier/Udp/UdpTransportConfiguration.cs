using System.Net;

namespace Dargon.Courier.TransportTier.Udp {
   public class UdpTransportConfiguration {
      public UdpTransportConfiguration(IPAddress multicastAddress, IPEndPoint multicastSendEndpoint, IPEndPoint multicastReceiveEndpoint, IPEndPoint unicastReceiveEndpoint) {
         MulticastAddress = multicastAddress;
         MulticastSendEndpoint = multicastSendEndpoint;
         MulticastReceiveEndpoint = multicastReceiveEndpoint;
         UnicastReceiveEndpoint = unicastReceiveEndpoint;
      }

      public IPAddress MulticastAddress { get; }
      public IPEndPoint MulticastSendEndpoint { get; }
      public IPEndPoint MulticastReceiveEndpoint { get; }
      public IPEndPoint UnicastReceiveEndpoint { get; }

      public static UdpTransportConfiguration Default => UdpTransportConfigurationBuilder.Create().Build();
   }

   public class UdpTransportConfigurationBuilder {
      private const int kDefaultPort = 21337;
      private static readonly IPAddress multicastAddress = IPAddress.Parse("235.13.33.37");
      private static readonly IPEndPoint multicastSendEndpoint = new IPEndPoint(multicastAddress, kDefaultPort);
      private static readonly IPEndPoint multicastReceiveEndpoint = new IPEndPoint(IPAddress.Any, kDefaultPort);
      private int unicastReceivePort = 21337;

      public UdpTransportConfigurationBuilder WithUnicastReceivePort(int p) {
         unicastReceivePort = p;
         return this;
      }

      public UdpTransportConfiguration Build() => new UdpTransportConfiguration(
         multicastAddress, 
         multicastSendEndpoint, 
         multicastReceiveEndpoint,
         new IPEndPoint(IPAddress.Any, unicastReceivePort));

      public static UdpTransportConfigurationBuilder Create() => new UdpTransportConfigurationBuilder();
   }
}