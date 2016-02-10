using Dargon.Commons;
using Dargon.Ryu.Modules;
using System;

namespace Dargon.Ryu.Internals {
   public class Activator : IActivator {
      public object ActivateRyuType(IRyuContainer ryu, RyuType type) {
         if (type.Activator != null) {
            return type.Activator(ryu);
         } else {
            return ActivateActivatorlessType(ryu, type.Type);
         }
      }

      public object ActivateActivatorlessType(IRyuContainer ryu, Type type) {
         try {
            var ctor = type.GetRyuConstructorOrThrow();
            var parameters = ctor.GetParameters();
            var arguments = parameters.Map(p => ryu.GetOrActivate(p.ParameterType));
            return ctor.Invoke(arguments);
         } catch (Exception e) {
            throw new RyuActivateException(type, e);
         }
      }
   }
}
