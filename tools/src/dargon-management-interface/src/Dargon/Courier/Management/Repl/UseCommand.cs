using System;
using System.Net;
using System.Threading;
using Dargon.Commons;
using Dargon.Courier.ManagementTier;
using Dargon.Courier.TransportTier.Tcp;
using Dargon.Courier.Utils;
using Dargon.Repl;
using Dargon.Ryu;

namespace Dargon.Courier.Management.Repl {
   public class UseCommand : DispatcherCommand {
      public UseCommand() : base("use") {
         var courierUdp = new CourierUdpClusterCommand();
         RegisterCommand(courierUdp);
         RegisterCommand(new AliasCommand("tcp", courierUdp));
      }

      public class CourierUdpClusterCommand : ICommand {
         public string Name => "courier-tcp";

         public int Eval(string args) {
            string ipAndPortString;
            args = Tokenizer.Next(args, out ipAndPortString);

            ipAndPortString = string.IsNullOrWhiteSpace(ipAndPortString) ? "127.0.0.1:21337" : ipAndPortString;

            IPEndPoint endpoint;
            if (!IPEndPointUtils.TryParseIpEndpoint(ipAndPortString, out endpoint)) {
               Console.Error.WriteLine($"Failed to parse '{ipAndPortString}' as tcp endpoint.");
               return 1;
            }

            Identity remoteIdentity = null;
            ManualResetEvent completionLatch = new ManualResetEvent(false);
            var courierFacade = CourierBuilder.Create(new RyuContainer(null, null))
                                              .UseTcpClientTransport(
                                                 endpoint.Address, 
                                                 endpoint.Port,
                                                 e => {
                                                    remoteIdentity = e.RemoteIdentity;
                                                    completionLatch.Set();
                                                 })
                                                 .BuildAsync().Result;

            Console.WriteLine($"Connecting to remote tcp endpoint {endpoint}.");
            completionLatch.WaitOne();
            Console.WriteLine($"Connected to {endpoint} with remote identity {remoteIdentity}.");

            var remotePeer = courierFacade.PeerTable.GetOrAdd(remoteIdentity.Id);
            var managementObjectService = courierFacade.RemoteServiceProxyContainer.Get<IManagementObjectService>(remotePeer);
            ReplGlobals.CourierFacade = courierFacade;
            ReplGlobals.ManagementObjectService = managementObjectService;

            return 0;
         }
      }
   }
}
