using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Ryu.Modules;

namespace Dargon.Ryu.Internals {
   public interface IActivator {
      Task<object> ActivateRyuTypeAsync(IRyuContainerInternal ryu, RyuType type, ActivationKind activationKind);
      Task<object> ActivateDefaultTypeAsync(IRyuContainerInternal ryu, Type type, ActivationKind activationKind);
   }
}
