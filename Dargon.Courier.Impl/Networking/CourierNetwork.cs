using Dargon.Courier.Identities;

namespace Dargon.Courier.Networking {
   public interface CourierNetwork {
      CourierNetworkContext Join(ReadableCourierEndpoint endpoint);
   }
}
