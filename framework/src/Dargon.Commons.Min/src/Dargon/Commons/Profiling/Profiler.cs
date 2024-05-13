using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Dargon.Commons.Algorithms;
using Dargon.Commons.Cli;
using Dargon.Commons.Collections;

namespace Dargon.Commons.Profiling;

public static class Profiler {
   [ThreadStatic] private static RuntimeProfilerCore tlsCurrentRuntimeProfilerCore;
   [ThreadStatic] private static RuntimeProfilerCore.Resources tlsReusableProfilerResources;

   private static ProfilingSession CreateSession() => new();

   public static RootScope Root(string name, out ProfilingSession session, bool subBlockCaseIsAggregate = false) {
      var res = Root(name, null, subBlockCaseIsAggregate);
      session = res.Session;
      return res;
   }

   public static RootScope Root([CallerMemberName] string name = null, ProfilingSession session = null, bool subBlockCaseIsAggregate = false) {
      var capture = tlsCurrentRuntimeProfilerCore;
      if (capture == null) {
         var resc = tlsReusableProfilerResources ??= new();
         var sessionWasSpecified = session != null;
         session ??= new ProfilingSession();
         capture = new RuntimeProfilerCore(resc, session, name);
         tlsCurrentRuntimeProfilerCore = capture;
         return new() {
            IsTrueRoot = true,
            Inst = capture,
            Session = session,
            DumpSessionOnDispose = !sessionWasSpecified,
         };
      } else {
         capture.EnterBlock(name, subBlockCaseIsAggregate);
         return new() {
            IsTrueRoot = false,
            Inst = capture,
            Session = null,
            DumpSessionOnDispose = false,
         };
      }
   }

   public struct RootScope : IDisposable {
      internal bool IsTrueRoot;
      internal RuntimeProfilerCore Inst;
      public ProfilingSession Session { get; internal set; }
      internal bool DumpSessionOnDispose;
      internal bool IsDisposed;

      public readonly void Step(string name) => Inst.EnterStep(name);

      public readonly BlockScope Block(string name) => BlockWithCapture(name, Inst, false);

      public readonly BlockScope Aggregate(string name) => BlockWithCapture(name, Inst, true);

      public void Cleanup() {
         Dispose();
      }

      public void CleanupAndDumpToConsole() {
         Cleanup();
         Session?.DumpToConsole();
      }

      public void Dispose() {
         if (IsDisposed) return;
         IsDisposed = true;

         if (IsTrueRoot) {
            Inst.ExitRoot();
            if (DumpSessionOnDispose) {
               Inst.DumpRoot();
            }

            tlsCurrentRuntimeProfilerCore = null;
         } else {
            Inst.ExitBlock();
         }
      }
   }

   public static BlockScope Block(string name) {
      return BlockInternal(name, tlsCurrentRuntimeProfilerCore, false);
   }

   private static BlockScope BlockInternal(string name, RuntimeProfilerCore capture, bool isIteration) {
      if (capture == null) {
         return new() { Inst = null };
      } else {
         return BlockWithCapture(name, capture, false);
      }
   }

   private static BlockScope BlockWithCapture(string name, RuntimeProfilerCore capture, bool isIteration) {
      capture.EnterBlock(name, isIteration);
      return new() { Inst = capture };
   }

   public struct BlockScope : IDisposable {
      internal RuntimeProfilerCore Inst;

      public readonly void Step(string name) => Inst?.EnterStep(name);

      public readonly BlockScope Block(string name) => BlockInternal(name, Inst, false);
      
      public readonly BlockScope Iteration(string name) => BlockInternal(name, Inst, true);

      public void Dispose() {
         Inst?.ExitBlock();
      }
   }

   public static class ProfilerExample {
      public static void BigTestPrime() {
         using var profiler = Profiler.Root(nameof(BigTestPrime));
         Test(10);
         Test(11);
      }

      public static void BigTestIter() {
         using var profiler = Profiler.Root(nameof(BigTestIter));
         for (var i = 10; i <= 11; i++) {
            using var _ = profiler.Aggregate("Aggr");
            Test(i);
         }
      }

      public static void Test(int i) {
         using var profiler = Profiler.Root(nameof(Test));

         profiler.Step("Init");
         //

         profiler.Step("If");
         var cond = DateTime.Now != default;
         if (i % 2 == 0) {
            using var _ = profiler.Block("Even");
            Thread.Sleep(200);
         } else {
            using var _ = profiler.Block("Odd");
            Thread.Sleep(100);
         }

         profiler.Step("Loop");
         for (var j = 0; j < i; j++) {
            using var _ = profiler.Aggregate("Iteration");
            Thread.Sleep(100);

            if (j % 2 == 0) {
               using var _2 = profiler.Block("Even");
               Thread.Sleep(20);
            } else {
               using var _2 = profiler.Block("Odd");
               Thread.Sleep(10);
               InnerBlock();
            }
         }

         profiler.Step("Cleanup");
         //
      }

      public static void InnerBlock() {
         using var profiler = Profiler.Block(nameof(InnerBlock));
         Thread.Sleep(10);

         {
            using var _ = profiler.Block("Render");
            Thread.Sleep(100);
         }
         {
            using var _ = profiler.Block("Step");
            Thread.Sleep(200);
         }
      }

      public static void TestSessions() {
         var session = Profiler.CreateSession();
         for (var i = 0; i < 10; i++) {
            using var profiler = Profiler.Root(nameof(TestSessions), session);
            InnerBlock();
         }

         session.DumpToConsole();
      }
   }
}

public class ProfilingSession {
   public RuntimeProfilerCore.BlockContext RootBlockContext;

   public void DumpToConsole() {
      RootBlockContext.DumpToConsole();
   }
}

public class RuntimeProfilerCore {
   private readonly Resources resources;
   private readonly BlockContext rootBlockContext;
   private BlockContext currentBlockContext;

   public RuntimeProfilerCore(Resources resources, ProfilingSession profilingSession, string rootBlockName) {
      this.resources = resources;
      resources.mainStopwatch.Restart();

      rootBlockContext = profilingSession.RootBlockContext ??= new() {
         Name = rootBlockName,
         IsStepElseIsBlock = false,
         ChildrenByName = new(),
         Parent = null,
         LastEnteredBlock = null,
         Precedence = new(),
         IsEntered = true,
         EnterTime = default,
         SumDuration = default,
         DurationCount = 0,
      };
      currentBlockContext = rootBlockContext;

      RecursivelyFlagAsUnentered(rootBlockContext);
      rootBlockContext.IsEntered = true;
   }

   public void EnterBlock(string name, bool isAggregateIteration) {
      BlockContext b;
      if (currentBlockContext.ChildrenByName.TryGetValue(name, out b)) {
         if (b.IsEntered) {
            if (!isAggregateIteration) {
               EnterBlock(name + "'", isAggregateIteration);
               return;
            }
         }
      }
      
      if (b == null) {
         b = new BlockContext {
            Name = name,
            IsStepElseIsBlock = false,
            ChildrenByName = new(),
            Parent = currentBlockContext,
            LastEnteredBlock = null,
            Precedence = new(),
            IsEntered = false,
            EnterTime = default,
            SumDuration = default,
            DurationCount = 0,
         };
         currentBlockContext.ChildrenByName[name] = b;
      }

      b.IsStepElseIsBlock.AssertIsFalse();

      if (isAggregateIteration) {
         RecursivelyFlagAsUnentered(b);
      }

      b.IsEntered.AssertIsFalse();
      b.EnterTime = resources.mainStopwatch.Elapsed;

      if (isAggregateIteration && currentBlockContext.LastEnteredBlock == b) {
         // no-op
      } else {
         currentBlockContext.Precedence.Add((currentBlockContext.LastEnteredBlock, b));
      }

      currentBlockContext.LastEnteredBlock = b;
      currentBlockContext = b;
      b.LastEnteredBlock = null;
      b.IsEntered = true;
   }

   private void RecursivelyFlagAsUnentered(BlockContext bc) {
      bc.IsEntered = false;
      bc.LastEnteredBlock = null;

      foreach (var (_, c) in bc.ChildrenByName) {
         RecursivelyFlagAsUnentered(c);
      }
   }

   public void EnterStep(string name) {
      if (currentBlockContext.IsStepElseIsBlock) {
         ExitStep();
         currentBlockContext.IsStepElseIsBlock.AssertIsFalse();
      }

      if (name.StartsWith("Test")) Console.WriteLine($"Test contained: {currentBlockContext.ChildrenByName.ContainsKey(name)}");
      BlockContext b;
      if (currentBlockContext.ChildrenByName.TryGetValue(name, out b)) {
         if (name.StartsWith("Test")) Console.WriteLine($"And is entered?: {b.IsEntered}");
         if (b.IsEntered) {
            if (name.StartsWith("Test")) Console.WriteLine($"Recurse to prime case");
            EnterStep(name + "'");
            return;
         }
      }

      if (b == null) {
         b = new BlockContext {
            Name = name,
            IsStepElseIsBlock = true,
            ChildrenByName = new(),
            Parent = currentBlockContext,
            LastEnteredBlock = null,
            Precedence = new(),
            IsEntered = false,
            EnterTime = default,
            SumDuration = default,
            DurationCount = 0,
         };
         currentBlockContext.ChildrenByName[name] = b;
      }

      b.IsStepElseIsBlock.AssertIsTrue();
      b.IsEntered.AssertIsFalse();
      currentBlockContext.Precedence.Add((currentBlockContext.LastEnteredBlock, b));
      currentBlockContext.LastEnteredBlock = b;
      currentBlockContext = b;
      b.LastEnteredBlock = null;
      b.IsEntered = true;
      b.EnterTime = resources.mainStopwatch.Elapsed;
   }

   public void ExitBlock() {
      if (currentBlockContext.IsStepElseIsBlock) {
         ExitStep();
      }

      currentBlockContext.IsStepElseIsBlock.AssertIsFalse();

      var now = resources.mainStopwatch.Elapsed;
      currentBlockContext.SumDuration += now - currentBlockContext.EnterTime;
      currentBlockContext.DurationCount++;
      currentBlockContext.EnterTime = default;
      currentBlockContext = currentBlockContext.Parent.AssertIsNotNull();
   }

   public void ExitStep() {
      currentBlockContext.IsStepElseIsBlock.AssertIsTrue();

      var now = resources.mainStopwatch.Elapsed;
      currentBlockContext.SumDuration += now - currentBlockContext.EnterTime;
      currentBlockContext.DurationCount++;
      currentBlockContext.EnterTime = default;

      currentBlockContext = currentBlockContext.Parent.AssertIsNotNull();
      currentBlockContext.IsStepElseIsBlock.AssertIsFalse();
   }

   public void ExitRoot() {
      if (currentBlockContext.IsStepElseIsBlock) {
         ExitStep();
      }

      currentBlockContext.AssertEquals(rootBlockContext);
      currentBlockContext.SumDuration += resources.mainStopwatch.Elapsed;
      currentBlockContext.DurationCount++;
   }

   public void DumpRoot() {
      rootBlockContext.DumpToConsole();
   }

   public class BlockContext {
      public string Name;
      public bool IsStepElseIsBlock;
      public Dictionary<string, BlockContext> ChildrenByName = new();
      public BlockContext Parent;
      public BlockContext LastEnteredBlock;
      public AddOnlyOrderedHashSet<(BlockContext, BlockContext)> Precedence;
      public bool IsEntered;
      public TimeSpan EnterTime;
      public TimeSpan SumDuration;
      public int DurationCount;

      public override string ToString() => $"[{Name}({IsStepElseIsBlock}, {ChildrenByName.Count})]";

      public void DumpToConsole() {
         void Visit(BlockContext n, int depth) {
            var indent = new string(' ', depth);

            var childrenDurationSum = TimeSpan.Zero;
            foreach (var c in n.ChildrenByName.Values) {
               childrenDurationSum += c.SumDuration;
            }

            var parentSumDuration = n.Parent?.SumDuration ?? TimeSpan.Zero;

            var exclusive = n.SumDuration - childrenDurationSum;
            var incMs = n.SumDuration.TotalMilliseconds;
            var excMs = exclusive.TotalMilliseconds;

            var line =
               n.DurationCount == 1
                  ? $"{indent}* {n.Name} {incMs:F2}ms ({excMs:F2}ms Exclusive)"
                  : $"{indent}* {n.Name} {incMs:F2}ms ({excMs:F2}ms Exclusive) in {n.DurationCount} iters => avg {(incMs / n.DurationCount):F2} ms/iter ({(excMs / n.DurationCount):F2} ms/iter Exclusive))";
            if (incMs > parentSumDuration.TotalMilliseconds * 0.3f) {
               using (ConsoleColorSwitch.Intensified()) {
                  Console.WriteLine(line);
               }
            } else {
               using (ConsoleColorSwitch.Dimmed()) {
                  Console.WriteLine(line);
               }
            }

            List<BlockContext> childOrder;
            try {
               childOrder = TopoSort.Compute(n.Precedence.ToList(), n.ChildrenByName.Values.ToList());
            } catch {
               Console.WriteLine($"{indent} => Precedence ({n.Precedence.Join(", ")})");
               throw;
            }

            foreach (var c in childOrder) {
               Visit(c, depth + 1);
            }
         }

         Visit(this, 0);
      }
   }

   public class Resources {
      public Stopwatch mainStopwatch = new();
      public List<Stopwatch> stopwatchPool = new();
   }
}