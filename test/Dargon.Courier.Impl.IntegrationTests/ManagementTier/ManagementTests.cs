using Dargon.Courier.ManagementTier;
using Dargon.Courier.TransportTier.Tcp.Server;
using Dargon.Courier.TransportTier.Test;
using Dargon.Courier.TransportTier.Udp;
using Dargon.Ryu;
using NMockito;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace Dargon.Courier.ServiceTier {
   public abstract class ManagementTestsBase : NMockitoInstance {
      private const string kTestMobGuid = "2FBA663C-3B5A-4B41-AF65-795E42B78270";
      private IRyuContainer courierContainer;

      public void Setup(IRyuContainer courierContainer) {
         this.courierContainer = courierContainer;
      }

      [Fact]
      public async Task RunAsync() {
         try {
            courierContainer.GetOrThrow<ManagementObjectService>()
                            .RegisterService(new TestMob());
            var managementObjectDirectoryService = courierContainer.GetOrThrow<ManagementObjectService>();

            var mobIdentifierDtos = managementObjectDirectoryService.EnumerateManagementObjects().ToList();
            AssertEquals(1, mobIdentifierDtos.Count);
            AssertEquals(Guid.Parse(kTestMobGuid), mobIdentifierDtos[0].Id);
            AssertEquals(typeof(TestMob).FullName, mobIdentifierDtos[0].FullName);

            var mobDescription = managementObjectDirectoryService.GetManagementObjectDescription(Guid.Parse(kTestMobGuid));
            AssertEquals(1, mobDescription.Methods.Count);
            AssertEquals("MethodName", mobDescription.Methods[0].Name);
            AssertEquals(0, mobDescription.Methods[0].Parameters.Count);
            AssertEquals(typeof(string), mobDescription.Methods[0].ReturnType);
         } catch (Exception e) {
            Console.WriteLine("Threw " + e);
            throw;
         } finally {
            await courierContainer.GetOrThrow<CourierFacade>().ShutdownAsync();
         }
      }

      [Guid(kTestMobGuid)]
      public class TestMob {
         [ManagedOperation]
         public string MethodName() => "";

         // (no [ManagedOperation])
         public string BadMethodName() => "";
      }
   }

   public class LocalManagementTests : ManagementTestsBase {
      public LocalManagementTests() {
         var root = new RyuFactory().Create();
         var testTransportFactory = new TestTransportFactory();
         var courierContainer = new CourierContainerFactory(root).CreateAsync(testTransportFactory).Result;
         Setup(courierContainer);
      }
   }

   public class UdpManagementTests : ManagementTestsBase {
      public UdpManagementTests() {
         var root = new RyuFactory().Create();
         var udpTransportFactory = new UdpTransportFactory();
         var courierContainer = new CourierContainerFactory(root).CreateAsync(udpTransportFactory).Result;
         Setup(courierContainer);
      }
   }

   public class TcpManagementTests : ManagementTestsBase {
      public TcpManagementTests() {
         var root = new RyuFactory().Create();
         var tcpTransportFactory = TcpTransportFactory.CreateServer(21337);
         var courierContainer = new CourierContainerFactory(root).CreateAsync(tcpTransportFactory).Result;
         Setup(courierContainer);
      }
   }
}
