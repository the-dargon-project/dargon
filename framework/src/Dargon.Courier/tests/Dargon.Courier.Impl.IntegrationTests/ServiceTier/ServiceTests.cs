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
using Dargon.Courier.AccessControlTier;
using Dargon.Courier.Vox;
using Dargon.Vox;
using NMockito.Expectations;
using Xunit;

namespace Dargon.Courier.ServiceTier {
   public abstract class ServiceTestsBase : NMockitoInstance {
      private CourierFacade clientFacade;
      private CourierFacade serverFacade;

      public ServiceTestsBase() {
         VoxGlobals.Serializer.ImportTypes(new CourierVoxTypes());
      }

      public void Setup(CourierFacade clientFacade, CourierFacade serverfacade) {
         this.clientFacade = clientFacade;
         this.serverFacade = serverfacade;
      }

      [Fact]
      public async Task RunAsync() {
         // ReSharper disable ExpressionIsAlwaysNull
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
               var expectedResult1 = (string)null;
               var expectedResult2 = CreatePlaceholder<string>();
               var expectedResult3a = (string)null;
               var expectedResult3b = CreatePlaceholder<string>();
               var expectedOutValue1 = CreatePlaceholder<string>();
               var expectedOutValue2 = CreatePlaceholder<int>();

               var serverInvokableService = CreateMock<IInvokableService>();
               Expect(serverInvokableService.Call1(param)).ThenReturn(expectedResult1);
               Expect<string, int, string>((x, y) => serverInvokableService.Call2(param, out x, out y))
                  .SetOut(expectedOutValue1, expectedOutValue2).ThenReturn(expectedResult2);
               Expect(serverInvokableService.Call3Async()).ThenResolve(expectedResult3a, expectedResult3b);

               serverFacade.LocalServiceRegistry.RegisterService(serverInvokableService);

               var remoteServiceProxyContainer = clientFacade.RemoteServiceProxyContainer;
               var clientInvokableService = remoteServiceProxyContainer.Get<IInvokableService>(clientSideServerContext);

               AssertEquals(expectedResult1, CourierClientRmiStatics.Async(() => clientInvokableService.Call1(param)).Result);
               AssertEquals(expectedResult2, clientInvokableService.Call2(param, out var outValue1, out var outValue2));
               AssertEquals(expectedResult3a, clientInvokableService.Call3Async().Result);
               AssertEquals(expectedResult3b, clientInvokableService.Call3Async().Result);
               AssertEquals(expectedOutValue1, outValue1);
               AssertEquals(expectedOutValue2, outValue2);
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
         string Call1(string arg1);
         string Call2(string arg1, out string out1, out int out2);
         Task<string> Call3Async();
      }
   }

   public class LocalServiceTests : ServiceTestsBase {
      public LocalServiceTests() {
         var testTransportFactory = new TestTransportFactory();

         var clientFacade = CourierBuilder.Create()
                                          .UseTransport(testTransportFactory)
                                          .UseGatekeeper(new PermitAllGatekeeper())
                                          .BuildAsync().Result;

         var serverFacade = CourierBuilder.Create()
                                          .UseTransport(testTransportFactory)
                                          .UseGatekeeper(new PermitAllGatekeeper())
                                          .BuildAsync().Result;

         Setup(clientFacade, serverFacade);
      }
   }

   public class UdpServiceTests : ServiceTestsBase {
      public UdpServiceTests() {
         var clientFacade = CourierBuilder.Create()
                                          .UseUdpTransport(
                                             UdpTransportConfigurationBuilder.Create()
                                                                             .WithUnicastReceivePort(21338)
                                                                             .Build())
                                          .UseGatekeeper(new PermitAllGatekeeper())
                                          .BuildAsync().Result;

         var serverFacade = CourierBuilder.Create()
                                          .UseUdpTransport(
                                             UdpTransportConfigurationBuilder.Create()
                                                                             .WithUnicastReceivePort(21339)
                                                                             .Build())
                                          .UseGatekeeper(new PermitAllGatekeeper())
                                          .BuildAsync().Result;

         Setup(clientFacade, serverFacade);
      }
   }

   public class TcpServiceTests : ServiceTestsBase {
      public TcpServiceTests() {
         var clientFacade = CourierBuilder.Create()
                                          .UseTcpClientTransport(IPAddress.Loopback, 21337)
                                          .UseGatekeeper(new PermitAllGatekeeper())
                                          .BuildAsync().Result;

         var serverFacade = CourierBuilder.Create()
                                          .UseTcpServerTransport(21337)
                                          .UseGatekeeper(new PermitAllGatekeeper())
                                          .BuildAsync().Result;

         Setup(clientFacade, serverFacade);
      }
   }
}
