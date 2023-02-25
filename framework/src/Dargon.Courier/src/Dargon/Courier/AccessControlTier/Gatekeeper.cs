using System.Reflection;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.ServiceTier.Vox;
using Dargon.Courier.SessionTier;
using Dargon.Courier.TransportTier.Tcp.Vox;

namespace Dargon.Courier.AccessControlTier {
   public interface IGatekeeper {
      /// <summary>
      /// Validate the remote identity.
      /// From this point onward, we can assume the identity can be trusted.
      /// The handshake might an access token, a refresh token, raw creds, etc.
      /// </summary>
      public void ValidateClientIdentity(WhoamiDto whoami);

      /// <summary>
      /// After we've validated the handshake, load the given session object.
      /// After this method call, the session is registered to global tables
      /// and begins receiving/sending data.
      /// </summary>
      public void LoadSessionState(WhoamiDto whoami, CourierAmbientPeerContext courierAmbientPeerContext);

      /// <summary>
      /// Validates the given inbound message event.
      /// For example, do access control here, validate the signature of the message.
      /// This call may be invoked in parallel for a given session.
      /// </summary>
      public void ValidateInboundMessageEvent<T>(IInboundMessageEvent<T> ime);

      /// <summary>
      /// Validates the given inbound service request.
      /// For example, do access control here, run argument validation.
      /// This call may be invoked in parallel for a given session.
      /// </summary>
      public void ValidateServiceRequest(IInboundMessageEvent<RmiRequestDto> req, object service, MethodInfo method, object[] args);
   }

   public class PermitAllGatekeeper : IGatekeeper {
      public void ValidateClientHandshake(HandshakeDto handshake) { }
      public void LoadSessionState(HandshakeDto handshake, CourierAmbientPeerContext courierAmbientPeerContext) { }
      public void ValidateClientIdentity(WhoamiDto whoami) {
         throw new System.NotImplementedException();
      }

      public void LoadSessionState(WhoamiDto whoami, CourierAmbientPeerContext courierAmbientPeerContext) {
         throw new System.NotImplementedException();
      }

      public void ValidateInboundMessageEvent<T>(IInboundMessageEvent<T> ime) { }
      public void ValidateServiceRequest(IInboundMessageEvent<RmiRequestDto> req, object service, MethodInfo method, object[] args) { }
   }
}
