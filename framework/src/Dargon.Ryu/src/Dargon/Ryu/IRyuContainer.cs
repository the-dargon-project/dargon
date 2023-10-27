using System;
using System.Collections.Generic;

namespace Dargon.Ryu {
   public interface IRyuContainer {
      /// <summary>
      /// Gets whatever is associated to the given type within the container.
      /// </summary>
      bool TryGet(Type type, out object value);

      /// <summary>
      /// Gets or activates an object of the given type associated
      /// within the container, tracked by the container.
      /// </summary>
      object GetOrActivate(Type type);

      /// <summary>
      /// Instantiates an object of the given type. This object will NOT be
      /// associated with the container!
      /// </summary>
      object ActivateUntracked(Type type);

      /// <summary>
      /// Instantiates an object of the given type. This object will NOT be
      /// associated with the container!
      ///
      /// Uses the additional dependencies for the creation of the object,
      /// though such dependencies also aren't stored in the container.
      /// </summary>
      object ActivateUntracked(Type type, Dictionary<Type, object> additionalDependencies);

      /// <summary>
      /// Finds all objects in the container of type extending, or equating
      /// to the given type.
      /// </summary>
      IEnumerable<object> Find(Type queryType);

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
      IRyuContainer CreateChildContainer();
   }
}
