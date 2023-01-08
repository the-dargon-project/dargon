using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dargon.Courier.TransportTier.Tcp.Server;

namespace Dargon.Courier.TransportTier.Tcp {
   public static class CourierBuilderExtensions {
      public static CourierBuilder UseTcpTransport(this CourierBuilder builder, TcpTransportConfiguration config) {
         if (config.Role == TcpRole.Server) {
            return builder.UseTransport(TcpTransportFactory.CreateServer(config.RemoteEndpoint.Port));
         } else {
            return builder.UseTransport(TcpTransportFactory.CreateClient(config.RemoteEndpoint.Address, config.RemoteEndpoint.Port));
         }
      }

      public static CourierBuilder UseTcpServerTransport(this CourierBuilder builder, int port) {
         return builder.UseTransport(TcpTransportFactory.CreateServer(port));
      }

      public static CourierBuilder UseTcpClientTransport(this CourierBuilder builder, IPAddress address, int port) {
         return builder.UseTransport(TcpTransportFactory.CreateClient(address, port));
      }

      public static CourierBuilder UseTcpClientTransport(this CourierBuilder builder, IPAddress address, int port, TcpTransportHandshakeCompletionHandler handshakeCompletionHandler) {
         return builder.UseTransport(TcpTransportFactory.CreateClient(address, port, handshakeCompletionHandler));
      }
   }
}
