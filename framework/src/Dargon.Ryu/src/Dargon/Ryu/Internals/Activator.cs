using Dargon.Commons;
using Dargon.Ryu.Modules;
using System;
using System.Linq;
using System.Reflection;
using Castle.Core.Internal;
using Dargon.Ryu.Attributes;

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
            var instance = ctor.Invoke(arguments);
            if (type.HasAttribute<InjectRequiredFields>()) {
               var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
               var fieldsToInitialize = type.GetFields(bindingFlags)
                                            .Where(f => f.IsInitOnly && !f.FieldType.IsValueType)
                                            .Where(f => f.GetValue(instance) == null);
               foreach (var field in fieldsToInitialize) {
                  field.SetValue(instance, ryu.GetOrActivate(field.FieldType));
               }
            }
            return instance;
         } catch (Exception e) {
            throw new RyuActivateException(type, e);
         }
      }
   }
}
