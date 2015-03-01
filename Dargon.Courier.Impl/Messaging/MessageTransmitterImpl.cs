using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.Identities;
using Dargon.Courier.Networking;
using Dargon.Courier.PortableObjects;
using ItzWarty;

namespace Dargon.Courier.Messaging {
   public class MessageTransmitterImpl : MessageTransmitter {
      private ReadableCourierEndpoint localEndpoint;
      private CourierNetworkContext networkContext;

      public MessageTransmitterImpl(ReadableCourierEndpoint localEndpoint, CourierNetworkContext networkContext) {
         this.localEndpoint = localEndpoint;
         this.networkContext = networkContext;
      }

      public void Transmit<TMessage>(TMessage message) {
         using (var ms = new MemoryStream())
         using (var writer = new BinaryWriter(ms)) {
            writer.Write();
         }
      }
   }
}
