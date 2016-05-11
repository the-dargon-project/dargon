using Dargon.Courier.ServiceTier.Server;
using Dargon.Courier.TestUtilities;
using Dargon.Ryu;
using NMockito;
using System;
using System.Runtime.InteropServices;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.ServiceTier.Client;
using NMockito.Utilities;
using Xunit;

namespace Dargon.Courier.ServiceTier {
   public class ServiceTests : NMockitoInstance {
      private readonly IRyuContainer clientContainer;
      private readonly IRyuContainer serverContainer;

      public ServiceTests() {
         var transport = new TestTransport();
         clientContainer = new CourierContainerFactory(new RyuFactory().Create()).Create(transport);
         serverContainer = new CourierContainerFactory(new RyuFactory().Create()).Create(transport);
      }

      [Fact]
      public void Run() {
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
            clientContainer.GetOrThrow<PeerTable>().GetOrAdd(
               serverContainer.GetOrThrow<Identity>().Id));

         string outValue;
         AssertEquals(expectedResult, clientInvokableService.Call(param, out outValue));
         AssertEquals(expectedOutValue, outValue);
         VerifyExpectationsAndNoMoreInteractions();
      }

      [Guid("7B862166-1E4C-4F54-83B0-082683B63EE1")]
      public interface IInvokableService {
         string Call(string arg1, out string out1);
      }
   }
}
