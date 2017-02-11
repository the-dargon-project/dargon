using Dargon.Courier;
using Dargon.Courier.TransportTier.Tcp;
using Dargon.Courier.TransportTier.Udp;
using Dargon.Hydrous.Impl;
using Dargon.Ryu;
using System;
using System.Threading.Tasks;
using static NMockito.NMockitoStatics;
using SCG = System.Collections.Generic;

namespace Dargon.Hydrous {
   public static class TestUtils {
      public static async Task<SCG.List<ICacheFacade<K, V>>> CreateCluster<K, V>(int cohortCount, Func<CacheConfiguration<K, V>> configurationFactory = null) {
         configurationFactory = configurationFactory ?? (() => new CacheConfiguration<K, V>("my-cache"));
         var cacheFacades = new SCG.List<ICacheFacade<K, V>>();
         for (var i = 0; i < cohortCount; i++) {
            cacheFacades.Add(await CreateCohortAsync<K, V>(i, configurationFactory()).ConfigureAwait(false));
         }
         return cacheFacades;
      }

      private static async Task<ICacheFacade<K, V>> CreateCohortAsync<K, V>(int cohortNumber, CacheConfiguration<K, V> cacheConfiguration) {
         // Force loads assemblies in directory and registers to global serializer.
         new RyuFactory().Create();

         AssertTrue(cohortNumber >= 0 && cohortNumber < 15);
         var cohortId = Guid.Parse("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx".Replace('x', (cohortNumber + 1).ToString("x")[0]));
         var courier = await CourierBuilder.Create()
                                           .ForceIdentity(cohortId)
                                           .UseUdpTransport(
                                              UdpTransportConfigurationBuilder.Create()
                                                                              .WithUnicastReceivePort(21338 + cohortNumber)
                                                                              .Build())
                                           .UseTcpServerTransport(21337 + cohortNumber)
                                           .BuildAsync().ConfigureAwait(false);
         var cacheInitializer = new CacheInitializer(courier);
         var myCacheFacade = cacheInitializer.CreateLocal<K, V>(cacheConfiguration);
         return myCacheFacade;
      }
   }
}
