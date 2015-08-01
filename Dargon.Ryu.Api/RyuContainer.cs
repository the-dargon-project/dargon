using System;

namespace Dargon.Ryu {
   public interface RyuContainer {
      /// <summary>
      /// Initializes the container.
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
      /// Constructs a new instance of T, even if T already exists in the
      /// container.
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <returns></returns>
      T Construct<T>();

      /// <summary>
      /// Constructs a new instance of the given type, even if an instance of
      /// the given type already exists in the container.
      /// container.
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
      /// </summary>
      T ForceConstruct<T>();

      /// <summary>
      /// Forces construction of the given type, ignoring whether it has been
      /// specified as a singleton.
      /// </summary>
      object ForceConstruct(Type type);
   }
}
