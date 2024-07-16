using Dargon.Commons;
using Dargon.Ryu.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Dargon.Commons.Exceptions;
using Dargon.Ryu.Attributes;
using Dargon.Ryu.Logging;

namespace Dargon.Ryu.Internals {
   public class Activator : IActivator {
      private readonly IRyuLogger logger;

      public Activator(IRyuLogger logger) {
         this.logger = logger;
      }

      public async Task<object> ActivateRyuTypeAsync(IRyuContainerInternal ryu, RyuType type, ActivationKind activationKind) {
         object inst;
         if (type.ActivatorAsync is {} activatorAsync) {
            inst = await activatorAsync(ryu.__A);
         } else if (type.ActivatorSync is { } activatorSync) {
            inst = activatorSync(ryu.__A);
         } else {
            inst = await ActivateDefaultTypeAsync(ryu, type.Type, activationKind);
         }
         type.HandleOnActivated(inst);
         return inst;
      }

      public async Task<object> ActivateDefaultTypeAsync(IRyuContainerInternal ryu, Type type, ActivationKind activationKind) {
         try {
            // throw if we find a DoNotAutoActivate attribute
            if (type.HasAttribute<RyuDoNotAutoActivate>()) {
               throw new InvalidOperationException($"The given type has a {nameof(RyuDoNotAutoActivate)} attribute, so it cannot be auto-activated.");
            }

            // create an uninitialized, empty object
            // RuntimeHelpers.GetUninitializedObject also exists on .NET Core, but
            // FormatterServices works for both framework & core (and proxies to the
            // same method on .NET Core)
#pragma warning disable SYSLIB0050
            var instance = FormatterServices.GetUninitializedObject(type);
#pragma warning restore SYSLIB0050

            // find the ryu constructor
            var ctor = type.GetRyuConstructorOrThrow();
            var parameters = ctor.GetParameters();

            //var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var dependencyFields = ReflectionCache.OfType(type)
                                                  .Fields
                                                  .FilterTo(f => f.HasAttribute<DependencyAttribute>());

            var fieldsToInject = new List<(FieldInfo field, Task<object> valTask)>();
            if (type.HasAttribute<InjectRequiredFields>()) {
               foreach (var field in dependencyFields) {
                  if (!field.IsInitOnly) {
                     throw new BadInputException($"[Dependency] Field {field.Name} of {type.FullName} was not marked InitOnly (readonly)!");
                  } else if (field.FieldType.GetTypeInfo().IsValueType) {
                     throw new BadInputException($"[Dependency] Field {field.Name} of {type.FullName} is of a value type, which cannot be injected.");
                  }

                  var fvTask = ryu.GetOrActivateAsync(field.FieldType, ActivationKind.DefaultActivatorDependency);
                  fieldsToInject.Add((field, fvTask));
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
            foreach (var (field, valTask) in fieldsToInject) {
               field.SetValue(instance, await valTask);
            }

            // invoke constructor after injecting fields
            var arguments = await parameters.MapParallelAsync(p => ryu.GetOrActivateAsync(p.ParameterType, activationKind));
            ctor.Invoke(instance, arguments);

            // inject fields after invoking ctor
            foreach (var (field, valTask) in fieldsToInject) {
               var readResult = field.GetValue(instance);
               var valResult = await valTask;
               if (readResult != valResult) {
                  logger.FieldModifiedByConstructorAfterInjection(field, valResult, readResult);
               }
               field.SetValue(instance, valResult);
            }

            return instance;
         } catch (Exception e) {
            throw new RyuActivateException(type, e);
         }
      }
   }
}
