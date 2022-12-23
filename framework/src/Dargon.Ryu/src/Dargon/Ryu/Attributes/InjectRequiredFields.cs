using System;

namespace Dargon.Ryu.Attributes {
   [AttributeUsage(AttributeTargets.Class)]
   public class InjectRequiredFields : Attribute { }

   [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
   public class RyuMemberAnnotation : Attribute { }

   [AttributeUsage(AttributeTargets.Field)]
   public class DependencyAttribute : RyuMemberAnnotation { }
}
