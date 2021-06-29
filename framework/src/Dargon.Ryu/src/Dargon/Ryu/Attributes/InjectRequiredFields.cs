using System;

namespace Dargon.Ryu.Attributes {
   [AttributeUsage(AttributeTargets.Class)]
   public class InjectRequiredFields : Attribute { }

   [AttributeUsage(AttributeTargets.Field)]
   public class DependencyAttribute : Attribute { }
}
