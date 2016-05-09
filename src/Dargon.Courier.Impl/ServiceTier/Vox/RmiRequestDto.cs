using System;
using Dargon.Vox;

namespace Dargon.Courier.ServiceTier.Vox {
   /// <summary>
   /// Remote Method Invocation Request Data Transfer Object
   /// </summary>
   [AutoSerializable]
   public class RmiRequestDto {
      public Guid InvocationId { get; set; }
      public Guid ServiceId { get; set; }
      public string MethodName { get; set; }
      public Type[] MethodGenericArguments { get; set; }
      public object[] MethodArguments { get; set; }
   }

   /// <summary>
   /// Remote Method Invocation Response Data Transfer Object
   /// </summary>
   public class RmiResponseDto {
      public Guid InvocationId { get; set; }
      public object ReturnValue { get; set; }
      public object[] Outs { get; set; }
      public object Exception { get; set; }
   }
}
