using System.Net;

namespace Dargon.Courier.TransportTier.Udp {
   public class UdpTransportConfiguration {
      private const int kDefaultPort = 21337;
      private static readonly IPAddress kDefaultMulticastAddress = IPAddress.Parse("235.13.33.37");
      private static readonly IPEndPoint kDefaultSendEndpoint = new IPEndPoint(kDefaultMulticastAddress, kDefaultPort);
      private static readonly IPEndPoint kDefaultReceiveEndpoint = new IPEndPoint(IPAddress.Any, kDefaultPort);
      private static readonly UdpTransportConfiguration kDefaultConfiguration = new UdpTransportConfiguration(kDefaultMulticastAddress, kDefaultSendEndpoint, kDefaultReceiveEndpoint);

      public UdpTransportConfiguration(IPAddress multicastAddress, IPEndPoint sendEndpoint, IPEndPoint receiveEndpoint) {
         MulticastAddress = multicastAddress;
         SendEndpoint = sendEndpoint;
         ReceiveEndpoint = receiveEndpoint;
      }

      public IPAddress MulticastAddress { get; }
      public IPEndPoint SendEndpoint { get; }
      public IPEndPoint ReceiveEndpoint { get; }

      public static UdpTransportConfiguration Default => kDefaultConfiguration;
   }
}