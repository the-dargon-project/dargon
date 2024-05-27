using System;

namespace Dargon.Ryu {
   [AttributeUsage(AttributeTargets.Constructor)]
   public class RyuConstructorAttribute : Attribute { }

   [AttributeUsage(AttributeTargets.Constructor)]
   public class RyuIgnoreConstructorAttribute : Attribute { }

   /// <summary>
   /// The given type should not be auto-activated; to be activated,
   /// it must be declared in an imported RyuModule w/ a custom activator
   /// callback function (as in, Ryu won't call the type's ctors and inject
   /// dependencies on its own).
   /// </summary>
   [AttributeUsage(AttributeTargets.Class)]
   public class RyuDoNotAutoActivate : Attribute { }
}