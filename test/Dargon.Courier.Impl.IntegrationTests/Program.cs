using Dargon.Commons;
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

         new BloomFilterTests().CanHaveNegligibleFalseCollisionRate();
         new BloomFilterTests().PerformanceTest();
         new MessagingTests().BroadcastTest().Wait();
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