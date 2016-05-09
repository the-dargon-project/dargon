using Dargon.Courier.ServiceTier.Vox;
using System;
using System.Text;
using Dargon.Commons;

namespace Dargon.Courier.ServiceTier.Client {
   public class RemoteException : Exception {
      public RemoteException() { }
      public RemoteException(string message) : base(message) { }

      public static RemoteException Create(Exception exception, RmiRequestDto body) {
         var sb = new StringBuilder();
         sb.AppendLine("== Courier-Proxied Remote Exception ==");
         sb.AppendLine("RequestId = " + body.InvocationId);
         sb.AppendLine("ServiceId = " + body.ServiceId);
         sb.AppendLine("Method = " + body.MethodName + "<" + body.MethodGenericArguments.Join(", ") + ">");
         sb.AppendLine("Exception = ...");
         sb.AppendLine(exception.ToString());
         sb.AppendLine();
         return new RemoteException(sb.ToString());
      }
   }
}
