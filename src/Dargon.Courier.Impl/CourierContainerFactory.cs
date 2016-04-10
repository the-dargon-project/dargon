using System.Threading.Tasks;
using Dargon.Ryu;
using Fody.Constructors;

namespace Dargon.Courier {
   [RequiredFieldsConstructor]
   public class CourierContainerFactory {
      private readonly IRyuContainer root = null;

      public void Create() {
         var child = root.CreateChildContainer();
      }
   }
}
