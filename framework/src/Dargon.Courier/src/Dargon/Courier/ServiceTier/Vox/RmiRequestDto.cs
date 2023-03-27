using System;
using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.ServiceTier.Vox {
   /// <summary>
   /// Remote Method Invocation Request Data Transfer Object
   /// </summary>
   [VoxType((int)CourierVoxTypeIds.RmiRequestDto)]
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
   [VoxType((int)CourierVoxTypeIds.RmiResponseDto)]
   public class RmiResponseDto {
      public Guid InvocationId { get; set; }
      public object ReturnValue { get; set; }
      public object[] Outs { get; set; }
      public object Exception { get; set; }
   }
}
