using Dargon.Commons;
using Dargon.Ryu.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Dargon.Commons.Exceptions;
using Dargon.Ryu.Attributes;
using Dargon.Ryu.Logging;

namespace Dargon.Ryu.Internals {
   public class Activator : IActivator {
      private readonly IRyuLogger logger;

      public Activator(IRyuLogger logger) {
         this.logger = logger;
      }

      public object ActivateRyuType(IRyuContainer ryu, RyuType type) {
         var inst = type.Activator?.Invoke(ryu) ?? ActivateDefaultType(ryu, type.Type);
         type.HandleOnActivated(inst);
         return inst;
      }

      public object ActivateDefaultType(IRyuContainer ryu, Type type) {
         try {
            // create an uninitialized, empty object
            var instance = FormatterServices.GetUninitializedObject(type);

            // find the ryu constructor
            var ctor = type.GetRyuConstructorOrThrow();
            var parameters = ctor.GetParameters();

            var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var dependencyFields = type.GetTypeInfo()
                                       .GetFields(bindingFlags)
                                       .Where(f => f.HasAttribute<DependencyAttribute>())
                                       .ToArray();

            var fieldsToInject = new List<(FieldInfo field, object val)>();
            if (type.HasAttribute<InjectRequiredFields>()) {
               foreach (var field in dependencyFields) {
                  if (!field.IsInitOnly) {
                     throw new BadInputException($"[Dependency] Field {field.Name} of {type.FullName} was not marked InitOnly (readonly)!");
                  } else if (field.FieldType.GetTypeInfo().IsValueType) {
                     throw new BadInputException($"[Dependency] Field {field.Name} of {type.FullName} is of a value type, which cannot be injected.");
                  }

                  fieldsToInject.Add((field, ryu.GetOrActivate(field.FieldType)));
               }
            } else if (dependencyFields.Length > 0) {
               throw new RyuInjectRequiredFieldsAttributeNotSpecifiedException(type);
            }

            // inject fields prior to ctor. Whether this works is actually technically iffy,
            // so constructor injection will always be the most "stable". Essentially, when you
            // set fields prior to invoking the constructor, you're presuming the constructor
            // won't then default-zero those fields. In fact, if you define a field and initialize
            // it to null, the codegen goes and sets the field to null. However, release builds
            // will have an optimization (compiler or runtime? not sure!) that elides these
            // unnecessary null sets, since object memory is presumed to be zeroed to begin with.
            //
            // In any case, the fields set here only consistently work if they look like:
            //
            //    [Dependency] private readonly T myField;
            //
            // No `= null` allowed.
            //
            // The other big caveat: You cannot inject an IRyuFacade like this.
            foreach (var (field, val) in fieldsToInject) {
               field.SetValue(instance, val);
            }

            // invoke constructor after injecting fields
            var arguments = parameters.Map(p => ryu.GetOrActivate(p.ParameterType));
            ctor.Invoke(instance, arguments);

            // inject fields after invoking ctor
            foreach (var (field, val) in fieldsToInject) {
               var readResult = field.GetValue(instance);
               if (readResult != val) {
                  logger.FieldModifiedByConstructorAfterInjection(field, val, readResult);
               }
               field.SetValue(instance, val);
            }

            return instance;
         } catch (Exception e) {
            throw new RyuActivateException(type, e);
         }
      }
   }
}
