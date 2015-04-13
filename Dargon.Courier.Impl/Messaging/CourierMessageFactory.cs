using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.PortableObjects;
using Dargon.PortableObjects;
using ItzWarty;

namespace Dargon.Courier.Messaging {
   public interface CourierMessageFactory {
      CourierMessageV1 CreateMessage(Guid recipient, MessageFlags messageFlags, object payload);
   }

   public class CourierMessageFactoryImpl : CourierMessageFactory {
      private readonly GuidProxy guidProxy;
      private readonly IPofSerializer pofSerializer;

      public CourierMessageFactoryImpl(GuidProxy guidProxy, IPofSerializer pofSerializer) {
         this.guidProxy = guidProxy;
         this.pofSerializer = pofSerializer;
      }

      public CourierMessageV1 CreateMessage(Guid recipient, MessageFlags messageFlags, object payload) {
         using (var ms = new MemoryStream())
         using (var writer = new BinaryWriter(ms)) {
            pofSerializer.Serialize(writer, payload);
            var messageId = guidProxy.NewGuid();
            return new CourierMessageV1(
               messageId,
               recipient,
               messageFlags,
               ms.GetBuffer(),
               0,
               (int)ms.Length
            );
         }
      }
   }
}
