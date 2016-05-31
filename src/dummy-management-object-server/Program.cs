using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Dargon.Commons;
using Dargon.Courier;
using Dargon.Courier.TransportTier.Tcp;
using Dargon.Courier.Vox;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;

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
   }

   public static class Program {
      public static void Main() {
         InitializeLogging();
         var courierFacade = CourierBuilder.Create()
                                           .UseTcpServerTransport(21337)
                                           .BuildAsync().Result;
         var testMob = new TestMob();
         courierFacade.ManagementObjectService.RegisterService(testMob);

         new CountdownEvent(1).Wait();
      }

      private static void InitializeLogging() {
         var config = new LoggingConfiguration();
         Target debuggerTarget = new DebuggerTarget() {
            Layout = "${longdate}|${level}|${logger}|${message} ${exception:format=tostring}"
         };
         Target consoleTarget = new ColoredConsoleTarget() {
            Layout = "${longdate}|${level}|${logger}|${message} ${exception:format=tostring}"
         };

#if !DEBUG
         debuggerTarget = new AsyncTargetWrapper(debuggerTarget);
         consoleTarget = new AsyncTargetWrapper(consoleTarget);
#else
         new AsyncTargetWrapper().Wrap(); // Placeholder for optimizing imports
#endif

         config.AddTarget("debugger", debuggerTarget);
         config.AddTarget("console", consoleTarget);

         var debuggerRule = new LoggingRule("*", LogLevel.Trace, debuggerTarget);
         config.LoggingRules.Add(debuggerRule);

         var consoleRule = new LoggingRule("*", LogLevel.Trace, consoleTarget);
         config.LoggingRules.Add(consoleRule);

         LogManager.Configuration = config;
      }
   }
}
