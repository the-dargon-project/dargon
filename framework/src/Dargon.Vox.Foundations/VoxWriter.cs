using System;
using System.IO;
using System.Text;

namespace Dargon.Vox2 {

   [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
   public class VarIntAttribute : VoxInternalBaseAttribute { }

   public class N : VoxInternalBaseDummyType { }

   public class N<T1> : VoxInternalBaseDummyType { }

   public class N<T1, T2> : VoxInternalBaseDummyType { }

   public class P : VoxInternalBaseDummyType { }

   public class P<T1> : VoxInternalBaseDummyType { }

   public class P<T1, T2> : VoxInternalBaseDummyType { }

   /// <summary>
   /// Polymorphic
   ///
   /// Within a type, fields default to non-polymorphic.
   /// To enable polymorphism (including null), they need to be annotated with [P]
   /// </summary>
   [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
   public class PAttribute : VoxInternalBaseAttribute { }

   /// <summary>Non-Polymorphic</summary>
   [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
   public class NAttribute : VoxInternalBaseAttribute { }

   // List
   public class L<T> : VoxInternalBaseDummyType { }

   // Dict
   public class D<T1, T2> : VoxInternalBaseDummyType { }
}