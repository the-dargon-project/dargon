using Dargon.Commons;
using Dargon.Courier.ServiceTier.Vox;
using Dargon.Vox;
using Fody.Constructors;
using System;
using System.Reflection;
using System.Text;

namespace Dargon.Courier.ServiceTier.Client {
   public class RemoteException : Exception, ISerializableType {
      public RemoteException() { }
      private RemoteException(string message) : base(message) { }

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

      public void Serialize(ISlotWriter writer) {
         writer.WriteString(0, Message);
      }

      public void Deserialize(ISlotReader reader) {
         GetType().GetField("_message", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, reader.ReadString(0));
      }
   }
}
