using System;

namespace Dargon.Ryu.Modules {
   [Flags]
   public enum RyuTypeFlags : uint {
      None                 = 0,

      /// <summary>
      /// This is a singleton, meaning the instance may be constructed by the container once
      /// and is cached for future usage.
      /// </summary>
      Singleton            = 0x00000001U,

      /// <summary>
      /// This instance should be constructed prior to module import completion
      /// </summary>
      Required             = 0x00000010U,

      /// <summary>
      /// This instance should be queued for construction prior to module import completion,
      /// such that other types can depend on it. However, upon module import completion the
      /// instance's state of activation is indeterminate.
      /// </summary>
      Eventual             = 0x00000020U,

      /// <summary>
      /// In parallel activation scenarios, this type must be activated on the main thread.
      /// </summary>
      RequiresMainThread   = 0x00000010U,

      /// <summary>
      /// The given type should not be default-activated by the container. For example,
      /// the user can inject the type via Set() or with an expliit <seealso cref="RyuTypeActivatorSync"/>
      /// from another module.
      ///
      /// For example, configs generally shouldn't be auto-activated as one
      /// would expect them to be explicitly injected.
      /// </summary>
      DenyDefaultActivate  = 0x00000100U,

      // /// <summary>
      // /// The given type cannot be explicitly activated by the user from non-module code.
      // /// (e.g. via container.Activate(T))
      // /// </summary>
      // DenyUserActivate = 0x00001000U,

      // /// <summary>
      // /// The given type cannot be activated as a dependency of a default-activator.
      // /// (e.g. container.Activate(X) invokes a default activator, which then wants to activate T)
      // /// </summary>
      // DenyDefaultActivatorDependency = 0x00002000U,

      // /// <summary>
      // /// The given type cannot be explicitly specified as a module dependency.
      // /// (e.g. prevent Require.Singleton(T))
      // /// </summary>
      // DenyModuleDependency = 0x00004000U,

      // /// <summary>
      // /// The given type cannot be activated due to a request from a user-specified activator.
      // /// </summary>
      // DenyUserActivatorDependency = 0x00008000U,

   }

   public enum ActivationKind {
      /// <summary>
      /// The type is being constructed as an auto-dependency for another type.
      /// </summary>
      DefaultActivatorDependency,

      /// <summary>
      /// The type is being constructed due to a user request.
      /// </summary>
      UserActivate,

      /// <summary>
      /// The type is being constructed as it is required by a module require.
      /// </summary>
      ModuleRequire,

      /// <summary>
      /// The type is being constructed as an explicit request from an activator.
      /// </summary>
      ExplicitActivatorDependency,
   }
}
