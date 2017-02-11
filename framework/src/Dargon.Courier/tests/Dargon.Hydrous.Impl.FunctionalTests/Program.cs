using Dargon.Commons;
using Dargon.Hydrous.Impl.Store.Postgre;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System;
using System.Threading.Tasks;
using Dargon.Courier;
using Dargon.Hydrous.Cache;
using Dargon.Hydrous.Impl.Store;
using Dargon.Hydrous.Store.Postgre;
using Dargon.Ryu;
using Dargon.Vox;

namespace Dargon.Hydrous {
   public class Program {
      public static void Main(string[] args) {
//         InitializeLogging();
//         var hitler = new PostgresHitler<int, WriteBehindFT.TestDto>("test", StaticTestConfiguration.PostgreConnectionString);
//         hitler.BatchUpdateAsync(
//            new[] {
//               new PendingUpdate<int, WriteBehindFT.TestDto> {
//                  Base = Entry<int, WriteBehindFT.TestDto>.CreateExistantWithValue(
//                     670997,
//                     new WriteBehindFT.TestDto {
//                        Name = "Fred"
//                     }),
//                  Updated = Entry<int, WriteBehindFT.TestDto>.CreateExistantWithValue(
//                     670997,
//                     new WriteBehindFT.TestDto {
//                        Name = "FredBanana"
//                     }),
//               },
//               new PendingUpdate<int, WriteBehindFT.TestDto> {
//                  Base = Entry<int, WriteBehindFT.TestDto>.CreateExistantWithValue(
//                     670998,
//                     new WriteBehindFT.TestDto {
//                        Name = "Banana"
//                     }),
//                  Updated = Entry<int, WriteBehindFT.TestDto>.CreateExistantWithValue(
//                     670998,
//                     new WriteBehindFT.TestDto {
//                        Name = "Patata"
//                     }),
//               },
//               new PendingUpdate<int, WriteBehindFT.TestDto> {
//                  Base = Entry<int, WriteBehindFT.TestDto>.CreateExistantWithValue(
//                     670999,
//                     new WriteBehindFT.TestDto {
//                     }),
//                  Updated = Entry<int, WriteBehindFT.TestDto>.CreateExistantWithValue(
//                     670999,
//                     new WriteBehindFT.TestDto {
//                     }),
//               },
//               new PendingUpdate<int, WriteBehindFT.TestDto> {
//                  Base = Entry<int, WriteBehindFT.TestDto>.CreateExistantWithValue(
//                     671000,
//                     new WriteBehindFT.TestDto {
//                     }),
//                  Updated = Entry<int, WriteBehindFT.TestDto>.CreateExistantWithValue(
//                     671000,
//                     new WriteBehindFT.TestDto {
//                     }),
//               },
//               new PendingUpdate<int, WriteBehindFT.TestDto> {
//                  Base = Entry<int, WriteBehindFT.TestDto>.CreateNonexistant(1000000),
//                  Updated = Entry<int, WriteBehindFT.TestDto>.CreateExistantWithValue(
//                     1000000,
//                     new WriteBehindFT.TestDto {
//                        Name = "asfdoij"
//                     })
//               },
//               new PendingUpdate<int, WriteBehindFT.TestDto> {
//                  Base = Entry<int, WriteBehindFT.TestDto>.CreateNonexistant(1000001),
//                  Updated = Entry<int, WriteBehindFT.TestDto>.CreateExistantWithValue(
//                     1000001,
//                     new WriteBehindFT.TestDto {
//                        Name = "asfdoijeqowiuj"
//                     })
//               }
//            }).Wait();
//         return;

         Console.BufferHeight = Int16.MaxValue - 1;
         new RyuFactory().Create();
         InitializeLogging();
//         new SomethingToDoWithEntryOperationChuggingAbstractBeanFactorySingletonTests().RunAsync().Wait();
//         new SingleNodeSingleWorkerWriteBehindFT().RunAsync().Wait();
         new MultipleNodeMultipleWorkerWriteBehindFT().RunAsync().Wait();
//         new WriteBehindFT().RunAsync().Wait();
//         while (true) GC.Collect();
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
