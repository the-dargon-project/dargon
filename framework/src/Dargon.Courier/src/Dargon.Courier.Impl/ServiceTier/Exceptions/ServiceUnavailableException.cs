using Dargon.Commons;
using Dargon.Courier.ServiceTier.Vox;
using System;

namespace Dargon.Courier.ServiceTier.Exceptions {
   public class ServiceUnavailableException : Exception {
      public ServiceUnavailableException(RmiRequestDto request) : base(GenerateErrorMessage(request)) {
      }

      public static string GenerateErrorMessage(RmiRequestDto request) {
         return $"Service of guid {request.ServiceId} unavailable (attempting to invoke method {request.MethodName}).";
      }
   }
}
