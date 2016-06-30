using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dargon.Courier.TransportTier.Tcp;
using Dargon.Courier.TransportTier.Test;
using Dargon.Courier.TransportTier.Udp;
using Dargon.Ryu;
using NMockito;
using Xunit;

namespace Dargon.Courier.ManagementTier {
   public abstract class ManagementTestsBase : NMockitoInstance {
      private const string kTestMobGuid = "2FBA663C-3B5A-4B41-AF65-795E42B78270";
      private CourierFacade courierFacade;

      public void Setup(CourierFacade courierFacade) {
         this.courierFacade = courierFacade;
      }

      [Fact]
      public async Task RunAsync() {
         try {
            courierFacade.MobOperations.RegisterMob(new TestMob());
            var managementObjectService = courierFacade.ManagementObjectService;

            var mobIdentifierDtos = managementObjectService.EnumerateManagementObjects().ToList();
            AssertEquals(1, mobIdentifierDtos.Count);
            AssertEquals(Guid.Parse(kTestMobGuid), mobIdentifierDtos[0].Id);
            AssertEquals(typeof(TestMob).FullName, mobIdentifierDtos[0].FullName);

            var mobDescription = managementObjectService.GetManagementObjectDescription(Guid.Parse(kTestMobGuid));
            AssertEquals(1, mobDescription.Methods.Count);
            AssertEquals("MethodName", mobDescription.Methods[0].Name);
            AssertEquals(0, mobDescription.Methods[0].Parameters.Count);
            AssertEquals(typeof(string), mobDescription.Methods[0].ReturnType);
         } catch (Exception e) {
            Console.WriteLine("Threw " + e);
            throw;
         } finally {
            await courierFacade.ShutdownAsync();
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
         var testTransportFactory = new TestTransportFactory();
         var courierContainer = CourierBuilder.Create()
                                              .UseTransport(testTransportFactory)
                                              .BuildAsync().Result;
         Setup(courierContainer);
      }
   }

   public class UdpManagementTests : ManagementTestsBase {
      public UdpManagementTests() {
         var courierContainer = CourierBuilder.Create()
                                              .UseUdpMulticastTransport()
                                              .BuildAsync().Result;

         Setup(courierContainer);
      }
   }

   public class TcpManagementTests : ManagementTestsBase {
      public TcpManagementTests() {
         var courierContainer = CourierBuilder.Create()
                                              .UseTcpServerTransport(21337)
                                              .BuildAsync().Result;

         Setup(courierContainer);
      }
   }
}
