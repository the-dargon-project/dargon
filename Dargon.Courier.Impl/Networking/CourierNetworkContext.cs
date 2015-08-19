using System.Net;

namespace Dargon.Courier.Networking {
   public interface CourierNetworkContext {
      void Broadcast(byte[] payload);
      void Broadcast(byte[] payload, int offset, int length);

      event DataArrivedHandler DataArrived;
   }

   public delegate void DataArrivedHandler(CourierNetwork network, byte[] data, int offset, int length, IPEndPoint remoteEndpoint);
}
