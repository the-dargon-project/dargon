using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Repl;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace Dargon.Courier.Management.UI {
   public class Program {
      public static void Main() {
         InitializeLogging();

         var dispatcher = new DispatcherCommand("root");
         dispatcher.RegisterCommand(new UseCommand());
         dispatcher.RegisterCommand(new FetchMobsCommand());
         dispatcher.RegisterCommand(new FetchOperationsCommand());
         dispatcher.RegisterCommand(new ChangeDirectoryCommand());
         dispatcher.RegisterCommand(new ListDirectoryCommand());
         dispatcher.RegisterCommand(new InvokeCommand());
         dispatcher.RegisterCommand(new SetCommand());
         dispatcher.RegisterCommand(new GetCommand());
         dispatcher.RegisterCommand(new TreeCommand());
         dispatcher.RegisterCommand(new ExitCommand());
         new ReplCore(dispatcher).Run();
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
