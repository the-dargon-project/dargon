namespace Dargon.Courier.Networking {
   public interface CourierNetworkContext {
      void Broadcast(byte[] payload);
      
      event DataArrivedHandler DataArrived;
   }

   public delegate void DataArrivedHandler(CourierNetwork network, byte[] data);
}
