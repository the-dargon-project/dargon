using System;
using System.Threading;
using Dargon.Commons;
using Dargon.Courier.ManagementTier;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.ServiceTier;
using Dargon.Courier.TransportTier;
using Dargon.Courier.Utilities;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace Dargon.Courier {
   public static class Program {
      public static void Main() => RunTests();

      public static void RunTests() {
         InitializeLogging();

//         Console.BufferHeight = 21337;
         new UdpMessagingTests().LargeObjectTest().Wait();
//         new LocalMessagingLoadTests().RunAsync().Wait();
//         new UdpServiceTests().RunAsync().Wait();
//         return;

//         new UdpMessagingTests().LargeObjectTest().Wait();
//         new UdpClientTests().Run();
//         return;

//         new UdpMessagingTests().LargeObjectTest().Wait();
         return;


         new BloomFilterTests().CanHaveNegligibleFalseCollisionRate();
         new BloomFilterTests().PerformanceTest();

         foreach (var messageTestType in new[] { typeof(LocalMessagingTests), typeof(TcpMessagingTests), typeof(UdpMessagingTests) }) {
            Console.Title = messageTestType.FullName;
            ((MessagingTestsBase)Activator.CreateInstance(messageTestType)).BroadcastTest().Wait();
            Console.Title += " !@#!# ";
            ((MessagingTestsBase)Activator.CreateInstance(messageTestType)).ReliableTest().Wait();
         }

         foreach (var serviceTestType in new[] { typeof(LocalServiceTests), typeof(TcpServiceTests), typeof(UdpServiceTests) }) {
            ((ServiceTestsBase)Activator.CreateInstance(serviceTestType)).RunAsync().Wait();
         }

         foreach (var managementTestType in new[] { typeof(LocalManagementTests), typeof(TcpManagementTests), typeof(UdpManagementTests) }) {
            ((ManagementTestsBase)Activator.CreateInstance(managementTestType)).RunAsync().Wait();
         }
      }

      private static void InitializeLogging() {
         var config = new LoggingConfiguration();
         Target debuggerTarget = new DebuggerTarget() {
            Layout = "${longdate}|${level}|${logger}|${message} ${exception:format=tostring}"
         };
         Target consoleTarget = new ColoredConsoleTarget() {
            Layout = "${longdate}|${level}|${logger}|${message} ${exception:format=tostring}"
         };

#if !DEBUG || TRUE
         debuggerTarget = new AsyncTargetWrapper(debuggerTarget);
         consoleTarget = new AsyncTargetWrapper(consoleTarget);
#else
         new AsyncTargetWrapper().Wrap(); // Placeholder for optimizing imports
#endif

         config.AddTarget("debugger", debuggerTarget);
         config.AddTarget("console", consoleTarget);

         var debuggerRule = new LoggingRule("*", LogLevel.Debug, debuggerTarget);
         config.LoggingRules.Add(debuggerRule);

         var consoleRule = new LoggingRule("*", LogLevel.Debug, consoleTarget);
         config.LoggingRules.Add(consoleRule);

         LogManager.Configuration = config;
      }
   }
}