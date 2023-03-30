using Dargon.Commons;
using Dargon.Courier;
using Dargon.Courier.ManagementTier;
using Dargon.Courier.TransportTier.Tcp;
using Dargon.Courier.Vox;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
#if ENABLE_UDP
using Dargon.Courier.TransportTier.Udp;
#endif

namespace dummy_management_object_server {
   [Guid("E6867903-3222-40ED-94BB-3C2C0FDB891B")]
   public class TestMob {
      [ManagedOperation]
      public int GetNext() {
         return Current++;
      }

      [ManagedOperation]
      public MessageDto GetMessage() => new MessageDto {
         Body = new List<object> { 1, 2, 3 },
         ReceiverId = Guid.NewGuid(),
         SenderId = Guid.NewGuid()
      };

      [ManagedOperation]
      public string SayHello(string name) => $"Hello, {name}!";

      [ManagedProperty]
      public int Current { get; set; }

      [ManagedProperty(IsDataSource = true)]
      public int Sin => (int)(100 * Math.Sin(DateTime.Now.ToUnixTimeMillis() * Math.PI * 2.0 / 30000) + 50);

      [ManagedProperty(IsDataSource = true)]
      public bool BoolSin => (100 * Math.Sin(DateTime.Now.ToUnixTimeMillis() * Math.PI * 2.0 / 30000) + 50) > 50;
   }

   public static class Program {
      public static void Main() {
         var courierFacade = CourierBuilder.Create()
#if ENABLE_UDP
                                           .UseUdpTransport()
#endif
                                           .UseTcpServerTransport(21337)
                                           .BuildAsync().Result;
         var testMob = new TestMob();
         courierFacade.MobOperations.RegisterMob(testMob);

         Console.WriteLine("Dummy MOB Server Initialized on:");

         foreach (var t in courierFacade.Transports) {
            Console.WriteLine(" - Transport: " + t.Description);
         }

         new CountdownEvent(1).Wait();
      }
   }
}
