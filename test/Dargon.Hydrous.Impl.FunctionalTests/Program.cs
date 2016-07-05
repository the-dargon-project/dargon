using Dargon.Commons;
using Dargon.Hydrous.Impl.Store.Postgre;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System;
using System.Threading.Tasks;
using Dargon.Hydrous.Cache;
using Dargon.Hydrous.Store.Postgre;
using Dargon.Ryu;
using Dargon.Vox;

namespace Dargon.Hydrous {
   public class Program {
      public static void Main(string[] args) {
         new RyuFactory().Create();
         InitializeLogging();
         new WriteBehindFT().RunAsync().Wait();
         while (true) GC.Collect();
         //         new IdfulValuePostgresOrmFT().RunAsync().Wait();
         //         new IdlessValuePostgresOrmFT().RunAsync().Wait();
         //         new CacheFT().CustomProcessTestAsync().Wait();
         //                   new CacheGetFT().RunAsync().Wait();
         //                   new PutFT().PutTestAsync().Wait();
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

         var debuggerRule = new LoggingRule("*", LogLevel.Warn, debuggerTarget);
         config.LoggingRules.Add(debuggerRule);

         var consoleRule = new LoggingRule("*", LogLevel.Warn, consoleTarget);
         config.LoggingRules.Add(consoleRule);

         LogManager.Configuration = config;
      }
   }
}
