using Dargon.Commons;
using Dargon.Courier;
using Dargon.Courier.TransportTier.Tcp;
using Dargon.Courier.TransportTier.Udp;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System;
using System.Threading.Tasks;

namespace Dargon.Hydrous.Impl {
   public interface ICacheFacade<K, V> {
      ICacheService<K, V> CacheService { get; }
      ICache<K, V> UserCache { get; }
   }
   
   public class CacheInitializer {
      private readonly CourierFacade courier;

      public CacheInitializer(CourierFacade courier) {
         this.courier = courier;
      }

      public ICacheFacade<K, V> CreateLocal<K, V>(CacheConfiguration<K, V> cacheConfiguration) {
         return CacheRoot<K, V>.Create(courier, cacheConfiguration);
      }
   }

   public class Program {
      public static void Main(string[] args) {
         Console.BufferHeight = 21337;
         InitializeLogging();
         RunAsync().Wait();
      }

      private static async Task RunAsync() {
         var courier = await CourierBuilder.Create()
                                           .UseUdpMulticastTransport()
                                           .UseTcpServerTransport(21337)
                                           .BuildAsync().ConfigureAwait(false);
         var cacheInitializer = new CacheInitializer(courier);
         var myCacheFacade = cacheInitializer.CreateLocal<int, string>(
            new CacheConfiguration<int, string>("my-cache"));
         var myCache = myCacheFacade.UserCache;
         var entry0 = await myCache.GetAsync(0).ConfigureAwait(false);
         var previous = await myCache.PutAsync(0, "asdf").ConfigureAwait(false);
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