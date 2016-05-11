using Dargon.Courier.ServiceTier.Server;
using Dargon.Courier.TestUtilities;
using Dargon.Ryu;
using NMockito;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Dargon.Commons;
using Dargon.Courier.ManagementTier;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.ServiceTier.Client;
using NMockito.Utilities;
using Xunit;

namespace Dargon.Courier.ServiceTier {
   public class ManagementTests : NMockitoInstance {
      private const string kTestMobGuid = "2FBA663C-3B5A-4B41-AF65-795E42B78270";
      private readonly IRyuContainer courierContainer;

      public ManagementTests() {
         var transport = new TestTransport();
         courierContainer = new CourierContainerFactory(new RyuFactory().Create()).Create(transport);
      }

      [Fact]
      public void Run() {
         courierContainer.GetOrThrow<ManagementObjectRegistry>()
                         .RegisterService(new TestMob());
         var managementObjectDirectoryService = courierContainer.GetOrThrow<ManagementObjectService>();

         var mobIds = managementObjectDirectoryService.EnumerateManagementObjectIds();
         AssertSequenceEquals(Guid.Parse(kTestMobGuid).Wrap(), mobIds);

         var mobDescription = managementObjectDirectoryService.GetManagementObjectDescription(Guid.Parse(kTestMobGuid));
         AssertEquals(1, mobDescription.Methods.Count);
         AssertEquals("MethodName", mobDescription.Methods[0].Name);
         AssertEquals(0, mobDescription.Methods[0].Parameters.Count);
         AssertEquals(typeof(string), mobDescription.Methods[0].ReturnType);
      }

      [Guid(kTestMobGuid)]
      public class TestMob {
         [ManagedOperation]
         public string MethodName() => "";

         // (no [ManagedOperation])
         public string BadMethodName() => "";
      }
   }
}
