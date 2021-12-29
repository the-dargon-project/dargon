using System;
using System.Windows.Forms;
using Dargon.Courier.Management.GUI.Views;
using NLog;
using NLog.Config;
using NLog.Targets;
using Views;

namespace Dargon.Courier.Management.GUI {
   public class Program {
      [STAThread]
      public static void Main(string[] args) {
         InitializeLogging();

         Console.WriteLine("Hello World!");
         Application.EnableVisualStyles();
         Application.Run(new MainWindow());
      }

      private static void InitializeLogging() {
         var config = new LoggingConfiguration();
         var debuggerTarget = new DebuggerTarget();
         config.AddTarget("debugger", debuggerTarget);

         var consoleTarget = new ConsoleTarget();
         config.AddTarget("console", consoleTarget);

         config.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, debuggerTarget));
         config.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, consoleTarget));
         LogManager.Configuration = config;
      }
   }
}
