using Dargon.Ryu;
using Fody.Constructors;

namespace Dargon.Courier {
   [RequiredFieldsConstructor]
   public class CourierContainerFactory {
      private readonly IRyuContainer root = null;

      public IRyuContainer Create() {
         var outboundBus = new AsyncEventBus<object>();
         var inboundBus = new AsyncEventBus<object>();
         var transport = UdpTransport.Create(outboundBus.Consumer(), inboundBus.Producer());

         var child = root.CreateChildContainer();
         child.Set(outboundBus.Producer());
         child.Set(inboundBus.Consumer());
         child.Set(transport);
         return child;
      }
   }
}
