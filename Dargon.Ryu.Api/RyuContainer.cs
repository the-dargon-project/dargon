using System;

namespace Dargon.Ryu {
   public interface RyuContainer {
      /// <summary>
      /// Initializes the container, force-loading all referenced assemblies 
      /// and instantiating all uninstantiated Ryu packages.
      /// </summary>
      void Setup();

      /// <summary>
      /// Gets or creates an instance of type T in the container. 
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <returns></returns>
      T Get<T>();

      /// <summary>
      /// Gets or creates an instance of the given type from the container.
      /// </summary>
      /// <param name="type"></param>
      /// <returns></returns>
      object Get(Type type);

      /// <summary>
      /// Sets the given instance as the implementation of the given type.
      /// 
      /// If the the type is already assigned within the container, then the
      /// old implementation is replaced with the newer implementation.
      /// </summary>
      void Set<T>(T instance);

      /// <summary>
      /// Sets the given instance as the implementation of the given type.
      /// 
      /// If the the type is already assigned within the container, then the
      /// old implementation is replaced with the newer implementation.
      /// </summary>
      void Set(Type type, object instance);

      /// <summary>
      /// Constructs a new instance of T, even if T already exists in the
      /// container.
      /// 
      /// Throws if the type is not registered, as it would then default to
      /// being a singleton. Likewise, throws if the type is marked cacheable,
      /// as that would result in the multiple instantiation of a singleton.
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <returns></returns>
      T Construct<T>();

      /// <summary>
      /// Constructs a new instance of the given type, even if an instance of
      /// the given type already exists in the container.
      /// container.
      /// 
      /// Throws if the type is not registered, as it would then default to
      /// being a singleton. Likewise, throws if the type is marked cacheable,
      /// as that would result in the multiple instantiation of a singleton.
      /// </summary>
      object Construct(Type type);

      /// <summary>
      /// Touches the given type, forcing Ryu to detect newly loaded 
      /// assemblies and the packages within them.
      /// </summary>
      /// <typeparam name="T"></typeparam>
      void Touch<T>();

      /// <summary>
      /// Touches the given type, forcing Ryu to detect newly loaded 
      /// assemblies and the packages within them.
      /// </summary>
      /// <param name="type"></param>
      void Touch(Type type);

      /// <summary>
      /// Forces construction of the given type, ignoring whether it has been
      /// specified as a singleton.
      /// 
      /// The method can be leveraged by containers and packages to force the
      /// initial construction of singletons.
      /// </summary>
      T ForceConstruct<T>();

      /// <summary>
      /// Forces construction of the given type, ignoring whether it has been
      /// specified as a singleton.
      /// 
      /// The method can be leveraged by containers and packages to force the
      /// initial construction of singletons.
      /// </summary>
      object ForceConstruct(Type type);
   }
}
