using System;
using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.ServiceTier.Vox {
   /// <summary>
   /// Remote Method Invocation Request Data Transfer Object
   /// </summary>
   [VoxType((int)CourierVoxTypeIds.RmiRequestDto)]
   public partial class RmiRequestDto {
      public Guid InvocationId { get; set; }
      public Guid ServiceId { get; set; }
      public string MethodName { get; set; }
      public Type[] MethodGenericArguments { get; set; }
      [L<P>] public object[] MethodArguments { get; set; }
   }

   /// <summary>
   /// Remote Method Invocation Response Data Transfer Object
   /// </summary>
   [VoxType((int)CourierVoxTypeIds.RmiResponseDto)]
   public partial class RmiResponseDto {
      public Guid InvocationId { get; set; }
      [P] public object ReturnValue { get; set; }
      [L<P>] public object[] Outs { get; set; }
      [P] public object Exception { get; set; }
   }
}
