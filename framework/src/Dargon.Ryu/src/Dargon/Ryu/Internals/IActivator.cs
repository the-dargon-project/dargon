using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Ryu.Modules;

namespace Dargon.Ryu.Internals {
   public interface IActivator {
      object ActivateRyuType(IRyuContainer ryu, RyuType type);
      object ActivateActivatorlessType(IRyuContainer ryu, Type type);
   }
}
