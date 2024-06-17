using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dargon.Ryu.Modules;

namespace Dargon.Ryu {
   public record struct TryGetResult<T>(bool Exists, T Value) {
      public bool Unpack(out T value) {
         value = Value;
         return Exists;
      }

      public static implicit operator TryGetResult<T>((bool, T) x) => new(x.Item1, x.Item2);
   }


   public interface IRyuContainerInternal {
      string Name { get; set; }
      IRyuContainer __U { get; }
      IRyuContainerForUserActivator __A { get; }

      /// <summary>
      /// Gets whatever is associated to the given type within the container.
      /// </summary>
      Task<TryGetResult<object>> TryGetAsync(Type type);

      /// <summary>
      /// Gets or activates an object of the given type associated
      /// within the container, tracked by the container.
      /// </summary>
      Task<object> GetOrActivateAsync(Type type, ActivationKind activationKind);

      /// <summary>
      /// <seealso cref="GetOrActivateAsync"/>
      ///
      /// The outer task completes when the the activation has been queued. If the queue happens within
      /// this call, then the caller's synchronization context is used for activation.
      /// The innter task completes when the activation has completed and its result is ready.
      /// </summary>
      Task<Task<object>> GetOrActivateAsyncExForModuleImport(Type type, ActivationKind activationKind);

      /// <summary>
      /// Instantiates an object of the given type. This object will NOT be
      /// associated with the container!
      /// </summary>
      Task<object> ActivateUntrackedAsync(Type type, ActivationKind activationKind);

      /// <summary>
      /// Instantiates an object of the given type. This object will NOT be
      /// associated with the container!
      ///
      /// Uses the additional dependencies for the creation of the object,
      /// though such dependencies also aren't stored in the container.
      /// </summary>
      Task<object> ActivateUntrackedAsync(Type type, Dictionary<Type, object> additionalDependencies, ActivationKind activationKind);

      /// <summary>
      /// Finds all objects in the container of type extending, or equating
      /// to the given type.
      /// </summary>
      Task<IEnumerable<object>> FindAsync(Type queryType, ActivationKind activationKind);

      /// <summary>
      /// Associates the given instance to the given type within the container.
      /// 
      /// If the the type is already assigned within the container, then the
      /// old implementation is replaced with the newer implementation.
      /// </summary>
      void Set(Type type, object instance);

      /// <summary>
      /// Creates a child container.
      /// </summary>
      IRyuContainer CreateChildContainer(string name);
   }

   /// <summary>
   /// User-facing API
   /// </summary>
   public interface IRyuContainer {
      string Name { get; set; }

      /// <summary>
      /// Gets whatever is associated to the given type within the container.
      /// </summary>
      Task<TryGetResult<object>> TryGetAsync(Type type);

      [Obsolete]
      bool TryGet(Type type, out object value) => TryGetAsync(type).Result.Unpack(out value);

      /// <summary>
      /// Gets or activates an object of the given type associated
      /// within the container, tracked by the container.
      /// </summary>
      Task<object> GetOrActivateAsync(Type type);

      [Obsolete]
      object GetOrActivate(Type type) => GetOrActivateAsync(type).Result;

      /// <summary>
      /// Instantiates an object of the given type. This object will NOT be
      /// associated with the container!
      /// </summary>
      Task<object> ActivateUntrackedAsync(Type type);

      [Obsolete]
      object ActivateUntracked(Type type) => ActivateUntrackedAsync(type).Result;

      /// <summary>
      /// Instantiates an object of the given type. This object will NOT be
      /// associated with the container!
      ///
      /// Uses the additional dependencies for the creation of the object,
      /// though such dependencies also aren't stored in the container.
      /// </summary>
      Task<object> ActivateUntrackedAsync(Type type, Dictionary<Type, object> additionalDependencies);

      [Obsolete]
      object ActivateUntracked(Type type, Dictionary<Type, object> additionalDependencies) => ActivateUntrackedAsync(type, additionalDependencies).Result;

      /// <summary>
      /// Finds all objects in the container of type extending, or equating
      /// to the given type.
      /// </summary>
      Task<IEnumerable<object>> FindAsync(Type queryType);

      [Obsolete]
      IEnumerable<object> Find(Type queryType) => FindAsync(queryType).Result;

      /// <summary>
      /// Associates the given instance to the given type within the container.
      /// 
      /// If the the type is already assigned within the container, then the
      /// old implementation is replaced with the newer implementation.
      /// </summary>
      void Set(Type type, object instance);

      /// <summary>
      /// Creates a child container.
      /// </summary>
      IRyuContainer CreateChildContainer(string name);
   }

   /// <summary>
   /// User-facing API
   /// </summary>
   public interface IRyuContainerForUserActivator {
      string Name { get; set; }

      /// <summary>
      /// Converts this reference to a ryu container meant for user-activation
      /// to a simple user-side reference to a ryu container.
      ///
      /// Activations will internally use a different ActivationKind.
      /// </summary>
      IRyuContainer AsUserRyuContainerUnsafe { get; }

      /// <summary>
      /// Gets whatever is associated to the given type within the container.
      /// </summary>
      Task<TryGetResult<object>> TryGetAsync(Type type);

      /// <summary>
      /// Gets or activates an object of the given type associated
      /// within the container, tracked by the container.
      /// </summary>
      Task<object> GetOrActivateAsync(Type type);

      /// <summary>
      /// Instantiates an object of the given type. This object will NOT be
      /// associated with the container!
      /// </summary>
      Task<object> ActivateUntrackedAsync(Type type);

      /// <summary>
      /// Instantiates an object of the given type. This object will NOT be
      /// associated with the container!
      ///
      /// Uses the additional dependencies for the creation of the object,
      /// though such dependencies also aren't stored in the container.
      /// </summary>
      Task<object> ActivateUntrackedAsync(Type type, Dictionary<Type, object> additionalDependencies);

      /// <summary>
      /// Finds all objects in the container of type extending, or equating
      /// to the given type.
      /// </summary>
      Task<IEnumerable<object>> FindAsync(Type queryType);

      /// <summary>
      /// Associates the given instance to the given type within the container.
      /// 
      /// If the the type is already assigned within the container, then the
      /// old implementation is replaced with the newer implementation.
      /// </summary>
      void Set(Type type, object instance);

      /// <summary>
      /// Creates a child container.
      /// </summary>
      IRyuContainer CreateChildContainer(string name);
   }
}
