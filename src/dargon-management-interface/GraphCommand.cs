using Dargon.Commons;
using Dargon.Courier.ManagementTier.Vox;
using Dargon.Repl;
using Dargon.Vox.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Dargon.Courier.AuditingTier;
using System.Diagnostics;

namespace Dargon.Courier.Management.UI {
   public class GraphCommand : ICommand {
      private const char kMu = '\u03BC';
      private const char kLowerBlock = '\u2584';
      private const char kUpperBlock = '\u2580';
      private const char kFullBlock = '\u2588';

      private delegate void EvalHelperFunc(SomeNode dataSetNode, DataSetDescriptionDto dataSetDto, bool plotDerivative);

      private static readonly IGenericFlyweightFactory<EvalHelperFunc> evalHelperFactory
         = GenericFlyweightFactory.ForMethod<EvalHelperFunc>(
            typeof(GraphCommand),
            nameof(EvalHelper));

      public string Name => "graph";

      public int Eval(string args) {
         string dataSetName;
         args = Tokenizer.Next(args, out dataSetName);

         if (dataSetName == "@demo-plot") {
            DemoPlot();
            return 0;
         } else if (dataSetName == "@demo-multi-plot") {
            DemoMultiPlot();
            return 0;
         }

         bool plotDerivative = false;
         while (!string.IsNullOrWhiteSpace(args)) {
            string flag;
            args = Tokenizer.Next(args, out flag);
            switch (flag) {
               case "-d":
                  plotDerivative = true;
                  break;
            }
         }

         SomeNode dataSetNode;
         if (!ReplGlobals.Current.TryGetChild(dataSetName, out dataSetNode)) {
            throw new Exception($"Couldn't find dataSet of name {dataSetNode}.");
         }

         var dataSetDto = dataSetNode.DataSetDto;

         if (dataSetDto == null) {
            throw new Exception($"Node {dataSetName} is not a data source.");
         }

         evalHelperFactory.Get(dataSetDto.ElementType)(dataSetNode, dataSetDto, plotDerivative);
         return 0;
      }

      private static void DemoMultiPlot() {
         MultiPlot(
            "My Title",
            new DataPoint<AggregateStatistics<double>>[] {
               new DataPoint<AggregateStatistics<double>> {
                  Time = DateTime.Now,
                  Value = new AggregateStatistics<double> {
                     Average = 1,
                     Count = 1,
                     Max = 1,
                     Min = 1,
                     Sum = 1
                  }
               },
               new DataPoint<AggregateStatistics<double>> {
                  Time = DateTime.Now - TimeSpan.FromSeconds(10),
                  Value = new AggregateStatistics<double> {
                     Average = 2,
                     Count = 3,
                     Max = 4,
                     Min = 5,
                     Sum = 6
                  }
               }
            }, false);
      }

      private static void DemoPlot() {
         var start = DateTime.Now;
         DrawSinglePlot(
            "Highlight at t-10 at value below normal.",
            Enumerable.Range(1, 100).Select(x => new PlotPoint { Value = x, Time = start - TimeSpan.FromSeconds(x) })
                      .Concat(Enumerable.Range(1, 100).Select(x => new PlotPoint { Value = x, Time = start - TimeSpan.FromSeconds(x) }))
                      .Concat(Enumerable.Range(10, 20).Select(x => new PlotPoint { Value = x - 2, Time = start - TimeSpan.FromSeconds(x) }))
                      .Concat(Enumerable.Range(10, 20).Select(x => new PlotPoint { Value = x - 2, Time = start - TimeSpan.FromSeconds(x) }))
                      .Concat(Enumerable.Range(10, 20).Select(x => new PlotPoint { Value = x - 2, Time = start - TimeSpan.FromSeconds(x) }))
                      .Concat(Enumerable.Range(10, 20).Select(x => new PlotPoint { Value = x - 2, Time = start - TimeSpan.FromSeconds(x) }))
                      .Concat(Enumerable.Range(10, 20).Select(x => new PlotPoint { Value = x - 2, Time = start - TimeSpan.FromSeconds(x) }))
                      .Concat(Enumerable.Range(10, 20).Select(x => new PlotPoint { Value = x - 2, Time = start - TimeSpan.FromSeconds(x) }))
                      .Concat(new PlotPoint { Value = 30, Time = start - TimeSpan.FromSeconds(20) }.Wrap()).ToArray(),
            Console.WindowWidth,
            Console.WindowHeight - 2,
            false);

         DrawSinglePlot(
            "My Title",
            Enumerable.Range(1, 100).Select(x => new PlotPoint { Value = (x - 50) * (x - 50), Time = start - TimeSpan.FromSeconds(x) }).ToArray(),
            Console.WindowWidth,
            Console.WindowHeight - 2,
            false);
      }

      private class PlotPoint {
         public DateTime Time { get; set; }
         public double Value { get; set; }
      }

      private static void EvalHelper<T>(SomeNode dataSetNode, DataSetDescriptionDto dataSetDto, bool plotDerivative) {
         var mobNode = dataSetNode.Parent;
         var mobDto = mobNode.MobDto;
         var dataSet = ReplGlobals.ManagementObjectService.GetManagedDataSet<T>(mobDto.FullName, dataSetNode.Name);
         var title = $"{mobDto.FullName}.{dataSetNode.Name}";

         if (typeof(T) == typeof(AggregateStatistics<int>)) {
            var stats = (DataPoint<AggregateStatistics<int>>[])(object)dataSet.DataPoints;
            var doubleStats = stats.Map(stat => new DataPoint<AggregateStatistics<double>> {
               Time = stat.Time,
               Value = new AggregateStatistics<double> {
                  Average = stat.Value.Average,
                  Count = stat.Value.Count,
                  Max = stat.Value.Max,
                  Min = stat.Value.Min,
                  Sum = stat.Value.Sum
               }
            });
            MultiPlot(title, doubleStats, plotDerivative);
         } else if (typeof(T) == typeof(AggregateStatistics<double>)) {
            var stats = (DataPoint<AggregateStatistics<double>>[])(object)dataSet.DataPoints;
            MultiPlot(title, stats, plotDerivative);
         } else {
            // dataset of ints or doubles
            var dataPoints = dataSet.DataPoints.Map(p => new PlotPoint { Time = p.Time, Value = Convert.ToDouble(p.Value) });
            int renderWidth = Console.WindowWidth;
            int renderHeight = Console.WindowHeight - 8;

            DrawSinglePlot(title, dataPoints, renderWidth, renderHeight, plotDerivative);
         }  
      }

      private static void MultiPlot(string title, DataPoint<AggregateStatistics<double>>[] dataPoints, bool plotDerivative) {
         // plot layout:
         // === title ===
         // [min][max][sum]
         // [averag][count]
         var renderWidth = Console.WindowWidth;
         var renderHeight = Console.WindowHeight - 2;
         for (var i = 0; i < renderHeight; i++) {
            Console.WriteLine();
         }

         // additional writeline necessary for after
         Console.WriteLine();

         var renderRightMargin = 1;
         var renderBottomMargin = 1;
         var thirdsRenderWidth = (renderWidth - 2 * renderRightMargin) / 3;
         var halvesRenderWidth = (renderWidth - 1 * renderRightMargin) / 2;
         var halvesRenderHeight = (renderHeight - 1 * renderBottomMargin) / 2;
         Console.CursorTop -= renderHeight + 1;

         var plotsTop = Console.CursorTop;

         if (title.Length < renderWidth) {
            title = " " + title + " ";
            title = title.PadLeft(title.Length + (renderWidth - title.Length) / 2, '=');
            title = title.PadRight(renderWidth, '=');
         }

         Console.SetCursorPosition(0, plotsTop);
         Console.Write(title);

         Console.SetCursorPosition(0, plotsTop + 1);
         DrawPlot("Min", dataPoints.Map(p => new PlotPoint { Time = p.Time, Value = p.Value.Min }), thirdsRenderWidth, halvesRenderHeight, plotDerivative);

         Console.SetCursorPosition(thirdsRenderWidth + renderRightMargin, plotsTop + 1);
         DrawPlot("Max", dataPoints.Map(p => new PlotPoint { Time = p.Time, Value = p.Value.Max }), thirdsRenderWidth, halvesRenderHeight, plotDerivative);

         Console.SetCursorPosition((thirdsRenderWidth + renderRightMargin) * 2, plotsTop + 1);
         DrawPlot("Sum", dataPoints.Map(p => new PlotPoint { Time = p.Time, Value = p.Value.Sum }), thirdsRenderWidth, halvesRenderHeight, plotDerivative);

         Console.SetCursorPosition(0, plotsTop + halvesRenderHeight + renderBottomMargin + 1);
         DrawPlot("Average", dataPoints.Map(p => new PlotPoint { Time = p.Time, Value = p.Value.Average }), halvesRenderWidth - renderRightMargin, halvesRenderHeight, plotDerivative);

         Console.SetCursorPosition(halvesRenderWidth, plotsTop + halvesRenderHeight + renderBottomMargin + 1);
         DrawPlot("Count", dataPoints.Map(p => new PlotPoint { Time = p.Time, Value = p.Value.Count }), halvesRenderWidth, halvesRenderHeight, plotDerivative);

         Console.Title = plotsTop.ToString();
         
         Console.WindowTop = Math.Min(plotsTop, Console.BufferHeight - Console.WindowHeight);

         // Write the additional writeline
         Console.SetCursorPosition(0, plotsTop + renderHeight - 1);
         Console.WriteLine();
      }

      private static void DrawSinglePlot(string title, IReadOnlyList<PlotPoint> dataPoints, int renderWidth, int renderHeight, bool plotDerivative) {
         for (var i = 0; i < renderHeight; i++) {
            Console.WriteLine();
         }

         // additional writeline necessary for afterward
         Console.WriteLine();

         Console.CursorTop -= renderHeight + 1;
         var plotsTop = Console.CursorTop;

         DrawPlot(title, dataPoints, renderWidth, renderHeight, plotDerivative);

         Console.WindowTop = Math.Min(plotsTop, Console.BufferHeight - Console.WindowHeight);

         // write the additional writeline again
         Console.SetCursorPosition(0, plotsTop + renderHeight - 1);
         Console.WriteLine();
      }

      private static void DrawPlot(string title, IReadOnlyList<PlotPoint> points, int renderWidth, int renderHeight, bool plotDerivative) {
         if (plotDerivative) {
            points = points.Zip(points.Skip(1), (a, b) => new PlotPoint {
               Time = b.Time,
               Value = (b.Value - a.Value) / (b.Time - a.Time).TotalSeconds
            }).ToArray();
         }

         int renderLeft = Console.CursorLeft;
         int renderTop = Console.CursorTop;

         if (title.Length < renderWidth) {
            title = title.PadLeft(title.Length + (renderWidth - title.Length) / 2);
            title = title.PadRight(renderWidth);
         }

         Console.SetCursorPosition(renderLeft, renderTop);
         Console.Write(title);
         
         var minValue = points.Min(p => p.Value);
         var maxValue = points.Max(p => p.Value);
         var minTime = points.Min(p => p.Time);
         var maxTime = points.Max(p => p.Time);
         var delta = maxValue - minValue;
         if (Math.Abs(delta) < Double.Epsilon) {
            delta = 1;
         }

         minValue -= delta * 0.1;
         maxValue += delta * 0.1;

         var maxLabel = NumberToLabelString(maxValue);
         var minLabel = NumberToLabelString(minValue);

         var rightAxisPadding = Math.Max(maxLabel.Length, minLabel.Length) + 2;

         var gw = renderWidth - rightAxisPadding;
         var gh = renderHeight - 4;
         var doubleGh = gh * 2; // used as we support drawing on lower and upper halves of character
         int[] upperBuffer = new int[gw * gh];
         int[] lowerBuffer = new int[gw * gh];
         for (int i = 0; i < points.Count; i++) {
            var point = points[i];
            var x = ((point.Time.Ticks - minTime.Ticks) * (gw - 1)) / (maxTime.Ticks - minTime.Ticks);
            var y = (int)(doubleGh * (point.Value - minValue) / (maxValue - minValue));
            var trueY = y / 2;
            var bufferIndex = (gh - trueY - 1) * gw + x;
            if (y % 2 == 0) {
               lowerBuffer[bufferIndex]++;
            } else {
               upperBuffer[bufferIndex]++;
            }
         }

         var orderedWeights = upperBuffer.Concat(lowerBuffer).Where(x => x != 0).OrderBy(x => x).ToArray();

         for (var y = 0; y < gh; y++) {
            Console.SetCursorPosition(renderLeft, renderTop + y + 1);
            var upperRow = Enumerable.Range(0, gw).Select(x => upperBuffer[y * gw + x]).ToArray();
            var lowerRow = Enumerable.Range(0, gw).Select(x => lowerBuffer[y * gw + x]).ToArray();
            for (var x = 0; x < gw; x++) {
               if (upperRow[x] != 0 || lowerRow[x] != 0) {
                  Console.SetCursorPosition(renderLeft + x, renderTop + y + 1);
//                  var upperColorIndex = Array.BinarySearch(orderedValues, upperRow[x]);
//                  var lowerColorIndex = Array.BinarySearch(orderedValues, lowerRow[x]);
                  var upperColor = PickWeightColor(upperRow[x], orderedWeights);
                  var lowerColor = PickWeightColor(lowerRow[x], orderedWeights);

                  using (new ConsoleColorSwitch().To(lowerColor, upperColor)) {
                     Console.Write(kLowerBlock);
                  }

//                  if (upperRow[x] == 1) {
//                     Console.Write(kLowerBlock);
//                  } else if (upperRow[x] == 2) {
//                     Console.Write(kUpperBlock);
//                  } else if (upperRow[x] == 3) {
//                     Console.Write(kFullBlock);
//                  }
               }
            }
            Console.SetCursorPosition(renderLeft + gw, renderTop + y + 1);
            Console.Write("|");
            if (y == 0) {
               Console.Write(maxLabel);
            } else if (y == gh - 1) {
               Console.Write(minLabel);
            }
         }

         Console.SetCursorPosition(renderLeft, renderTop + gh + 1);
         Console.Write("-".Repeat(gw) + "+");
         var minTimeString = TimeToRelativeString(minTime);
         var maxTimeString = TimeToRelativeString(maxTime);

         Console.SetCursorPosition(renderLeft, renderTop + gh + 2);
         Console.Write(minTimeString + " ".Repeat(gw - minTimeString.Length - maxTimeString.Length + 1) + maxTimeString);

         Console.SetCursorPosition(renderLeft, renderTop + gh + 3);
         var statisticsString = $"^: {NumberToLabelString(points.Max(p => p.Value))}, v: {NumberToLabelString(points.Min(p => p.Value))},  : {NumberToLabelString(points.Sum(p => p.Value) / points.Count)}";
         Console.Write(statisticsString.PadRight(renderWidth - 1).Substring(0, renderWidth - 1));

         Trace.Assert(Console.CursorTop == renderTop + renderHeight - 1);
         Console.WriteLine();
      }

      private static ConsoleColor PickWeightColor(int value, int[] orderedValues) {
         if (value == 0) {
            return ConsoleColor.Black;
         }
         var index = Array.BinarySearch(orderedValues, value);
         if (index < orderedValues.Length / 3) {
            return ConsoleColor.DarkGray;
         } else if (index < 2 * orderedValues.Length / 3) {
            return ConsoleColor.Gray;
         } else {
            return ConsoleColor.White;
         }
      }

      private static string NumberToLabelString(double value) {
         if (Math.Abs(value) > 1E7 || (value != 0 && Math.Abs(value) < 1E-3)) {
            return value.ToString("E3");
         } else {
            return value.ToString("F2");
         }
      }

      private static string TimeToRelativeString(DateTime t) {
         var dt = DateTime.Now - t;
         if (dt < TimeSpan.FromSeconds(1)) {
            return dt.TotalMilliseconds.ToString("F1") + "ms ago";
         } else if (dt < TimeSpan.FromMinutes(1)) {
            return dt.TotalSeconds.ToString("F1") + "s ago";
         } else if (dt < TimeSpan.FromMinutes(10)) {
            return dt.TotalMinutes.ToString("F1") + "m ago";
         } else {
            return dt.TotalHours.ToString("F1") + "h ago";
         }
      }
   }
}
