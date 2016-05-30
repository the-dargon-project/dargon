using Dargon.Courier.PeeringTier;
using Dargon.Courier.ServiceTier.Client;
using Dargon.Courier.ServiceTier.Server;
using Dargon.Courier.TransportTier.Test;
using Dargon.Ryu;
using NMockito;
using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Courier.TransportTier.Tcp.Server;
using Dargon.Courier.TransportTier.Udp;
using Xunit;

namespace Dargon.Courier.ServiceTier {
   public abstract class ServiceTestsBase : NMockitoInstance {
      private IRyuContainer clientContainer;
      private IRyuContainer serverContainer;
      
      public void Setup(IRyuContainer clientContainer, IRyuContainer serverContainer) {
         this.clientContainer = clientContainer;
         this.serverContainer = serverContainer;
      }

      [Fact]
      public async Task RunAsync() {
         try {
            using (var timeout = new CancellationTokenSource(213333337)) {
               // await discovery between nodes
               var clientSideServerContext = clientContainer.GetOrThrow<PeerTable>().GetOrAdd(serverContainer.GetOrThrow<Identity>().Id);
               await clientSideServerContext.WaitForDiscoveryAsync(timeout.Token).ConfigureAwait(false);

               var serverSideClientContext = serverContainer.GetOrThrow<PeerTable>().GetOrAdd(clientContainer.GetOrThrow<Identity>().Id);
               await serverSideClientContext.WaitForDiscoveryAsync(timeout.Token).ConfigureAwait(false);

               Console.WriteLine("CSSC " + clientSideServerContext.Discovered + " " + clientSideServerContext.Identity.Id);
               Console.WriteLine("SSCC " + serverSideClientContext.Discovered + " " + serverSideClientContext.Identity.Id);

               var param = CreatePlaceholder<string>();
               var expectedResult = CreatePlaceholder<string>();
               var expectedOutValue = CreatePlaceholder<string>();

               var serverInvokableService = CreateMock<IInvokableService>();
               Expect<string, string>(x => serverInvokableService.Call(param, out x))
                  .SetOut(expectedOutValue).ThenReturn(expectedResult);

               var serviceRegistry = serverContainer.GetOrThrow<LocalServiceRegistry>();
               serviceRegistry.RegisterService(serverInvokableService);

               var remoteServiceProxyContainer = clientContainer.GetOrThrow<RemoteServiceProxyContainer>();
               var clientInvokableService = remoteServiceProxyContainer.Get<IInvokableService>(
                  clientSideServerContext);

               string outValue;
               AssertEquals(expectedResult, clientInvokableService.Call(param, out outValue));
               AssertEquals(expectedOutValue, outValue);
               VerifyExpectationsAndNoMoreInteractions();
            }
         } catch (Exception e) {
            Console.WriteLine("Threw " + e);
            throw;
         } finally {
            await serverContainer.GetOrThrow<CourierFacade>().ShutdownAsync();
            await clientContainer.GetOrThrow<CourierFacade>().ShutdownAsync();
         }
      }

      [Guid("7B862166-1E4C-4F54-83B0-082683B63EE1")]
      public interface IInvokableService {
         string Call(string arg1, out string out1);
      }
   }

   public class LocalServiceTests : ServiceTestsBase {
      public LocalServiceTests() {
         var root = new RyuFactory().Create();
         var testTransportFactory = new TestTransportFactory();
         var clientContainer = new CourierContainerFactory(root).CreateAsync(testTransportFactory).Result;
         var serverContainer = new CourierContainerFactory(root).CreateAsync(testTransportFactory).Result;
         Setup(clientContainer, serverContainer);
      }
   }

   public class UdpServiceTests : ServiceTestsBase {
      public UdpServiceTests() {
         var clientContainer = new CourierContainerFactory(new RyuFactory().Create()).CreateAsync(new UdpTransportFactory()).Result;
         var serverContainer = new CourierContainerFactory(new RyuFactory().Create()).CreateAsync(new UdpTransportFactory()).Result;
         Setup(clientContainer, serverContainer);
      }
   }

   public class TcpServiceTests : ServiceTestsBase {
      public TcpServiceTests() {
         var clientContainer = new CourierContainerFactory(new RyuFactory().Create()).CreateAsync(TcpTransportFactory.CreateClient(IPAddress.Loopback, 21337)).Result;
         var serverContainer = new CourierContainerFactory(new RyuFactory().Create()).CreateAsync(TcpTransportFactory.CreateServer(21337)).Result;
         Setup(clientContainer, serverContainer);
      }
   }
}
