using System;
using System.Text;
using Dargon.Commons;
using Dargon.Courier.ServiceTier.Vox;
using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.ServiceTier.Client {
   [VoxType((int)CourierVoxTypeIds.RemoteException, Flags = VoxTypeFlags.StubRaw)]
   public partial class RemoteException : Exception {
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

      public static partial void Stub_WriteRaw_RemoteException(VoxWriter vw, RemoteException x) {
         vw.WriteRawString(x.Message);
      }
      public static partial void Stub_ReadRawIntoRef_RemoteException(VoxReader vr, ref RemoteException x) {
         x = new RemoteException(vr.ReadRawString());
      }

      // public void Serialize(IBodyWriter writer) {
      //    writer.Write(Message);
      // }
      //
      // public void Deserialize(IBodyReader reader) {
      //    GetType().GetField("_message", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, reader.Read<string>());
      // }
   }
}