using Dargon.Courier.PeeringTier;
using Dargon.Courier.ServiceTier.Client;
using Dargon.Courier.ServiceTier.Server;
using Dargon.Courier.TransportTier.Tcp;
using Dargon.Courier.TransportTier.Test;
using Dargon.Courier.TransportTier.Udp;
using Dargon.Ryu;
using NMockito;
using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Dargon.Courier.ServiceTier {
   public abstract class ServiceTestsBase : NMockitoInstance {
      private CourierFacade clientFacade;
      private CourierFacade serverFacade;

      public void Setup(CourierFacade clientFacade, CourierFacade serverfacade) {
         this.clientFacade = clientFacade;
         this.serverFacade = serverfacade;
      }

      [Fact]
      public async Task RunAsync() {
         try {
            using (var timeout = new CancellationTokenSource(213333337)) {
               // await discovery between nodes
               var clientSideServerContext = clientFacade.PeerTable.GetOrAdd(serverFacade.Identity.Id);
               await clientSideServerContext.WaitForDiscoveryAsync(timeout.Token).ConfigureAwait(false);

               var serverSideClientContext = serverFacade.PeerTable.GetOrAdd(clientFacade.Identity.Id);
               await serverSideClientContext.WaitForDiscoveryAsync(timeout.Token).ConfigureAwait(false);

               Console.WriteLine("CSSC " + clientSideServerContext.Discovered + " " + clientSideServerContext.Identity.Id);
               Console.WriteLine("SSCC " + serverSideClientContext.Discovered + " " + serverSideClientContext.Identity.Id);

               var param = CreatePlaceholder<string>();
               var expectedResult = CreatePlaceholder<string>();
               var expectedOutValue = CreatePlaceholder<string>();

               var serverInvokableService = CreateMock<IInvokableService>();
               Expect<string, string>(x => serverInvokableService.Call(param, out x))
                  .SetOut(expectedOutValue).ThenReturn(expectedResult);

               serverFacade.LocalServiceRegistry.RegisterService(serverInvokableService);

               var remoteServiceProxyContainer = clientFacade.RemoteServiceProxyContainer;
               var clientInvokableService = remoteServiceProxyContainer.Get<IInvokableService>(clientSideServerContext);

               string outValue;
               AssertEquals(expectedResult, clientInvokableService.Call(param, out outValue));
               AssertEquals(expectedOutValue, outValue);
               VerifyExpectationsAndNoMoreInteractions();
            }
         } catch (Exception e) {
            Console.WriteLine("Threw " + e);
            throw;
         } finally {
            await serverFacade.ShutdownAsync();
            await clientFacade.ShutdownAsync();
         }
      }

      [Guid("7B862166-1E4C-4F54-83B0-082683B63EE1")]
      public interface IInvokableService {
         string Call(string arg1, out string out1);
      }
   }

   public class LocalServiceTests : ServiceTestsBase {
      public LocalServiceTests() {
         var testTransportFactory = new TestTransportFactory();

         var clientFacade = CourierBuilder.Create()
                                          .UseTransport(testTransportFactory)
                                          .BuildAsync().Result;

         var serverFacade = CourierBuilder.Create()
                                          .UseTransport(testTransportFactory)
                                          .BuildAsync().Result;

         Setup(clientFacade, serverFacade);
      }
   }

   public class UdpServiceTests : ServiceTestsBase {
      public UdpServiceTests() {
         var clientFacade = CourierBuilder.Create()
                                          .UseUdpMulticastTransport()
                                          .BuildAsync().Result;

         var serverFacade = CourierBuilder.Create()
                                          .UseUdpMulticastTransport()
                                          .BuildAsync().Result;

         Setup(clientFacade, serverFacade);
      }
   }

   public class TcpServiceTests : ServiceTestsBase {
      public TcpServiceTests() {
         var clientFacade = CourierBuilder.Create()
                                          .UseTcpClientTransport(IPAddress.Loopback, 21337)
                                          .BuildAsync().Result;

         var serverFacade = CourierBuilder.Create()
                                          .UseTcpServerTransport(21337)
                                          .BuildAsync().Result;

         Setup(clientFacade, serverFacade);
      }
   }
}
