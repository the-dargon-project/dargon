using System;
using Dargon.Ryu.Internals;

namespace Dargon.Ryu {
   public interface IRyuFacade : IRyuContainer {
      IRyuContainer Container { get; }
      object Activate(Type type);
   }
}
