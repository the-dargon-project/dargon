using Dargon.PortableObjects;

namespace Dargon.Ryu {
   public class RyuFactory {
      public RyuContainer Create() {
         var pofContext = new PofContext();
         var pofSerializer = new PofSerializer(pofContext);
         return new RyuContainerImpl(pofContext, pofSerializer);
      }
   }
}