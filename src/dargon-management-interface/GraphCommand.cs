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
      private delegate void EvalHelperFunc(SomeNode dataSetNode, DataSetDescriptionDto dataSetDto, bool plotDerivative);

      private static readonly IGenericFlyweightFactory<EvalHelperFunc> evalHelperFactory
         = GenericFlyweightFactory.ForMethod<EvalHelperFunc>(
            typeof(GraphCommand),
            nameof(EvalHelper));

      public string Name => "graph";

      public int Eval(string args) {
//         MultiPlot(
//            "My Title",
//            new DataPoint<AggregateStatistics<double>>[] {
//               new DataPoint<AggregateStatistics<double>> {
//                  Time = DateTime.Now,
//                  Value = new AggregateStatistics<double> {
//                     Average = 1,
//                     Count = 1,
//                     Max = 1,
//                     Min = 1,
//                     Sum = 1
//                  }
//               },
//               new DataPoint<AggregateStatistics<double>> {
//                  Time = DateTime.Now - TimeSpan.FromSeconds(10),
//                  Value = new AggregateStatistics<double> {
//                     Average = 2,
//                     Count = 3,
//                     Max = 4,
//                     Min = 5,
//                     Sum = 6
//                  }
//               }
//            }, false);
//         return 0;

         string dataSetName;
         args = Tokenizer.Next(args, out dataSetName);

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

            for (var i = 0; i < renderHeight; i++) {
               Console.WriteLine();
            }

            Console.CursorTop -= renderHeight;
            var plotsTop = Console.CursorTop;
            
            DrawPlot(title, dataPoints, renderWidth, renderHeight, plotDerivative);

            Console.WindowTop = Math.Min(plotsTop, Console.BufferHeight - Console.WindowHeight);
         }  
      }

      private static void MultiPlot(string title, DataPoint<AggregateStatistics<double>>[] dataPoints, bool plotDerivative) {
         // plot layout:
         // === title ===
         // [min][max][sum]
         // [averag][count]
         var renderWidth = Console.WindowWidth;
         var renderHeight = Console.WindowHeight - 3;
         for (var i = 0; i < renderHeight; i++) {
            Console.WriteLine();
         }

         var renderRightMargin = 1;
         var renderBottomMargin = 1;
         var thirdsRenderWidth = (renderWidth - 2 * renderRightMargin) / 3;
         var halvesRenderWidth = (renderWidth - 1 * renderRightMargin) / 2;
         var halvesRenderHeight = (renderHeight - 1 * renderBottomMargin) / 2;
         Console.CursorTop -= renderHeight;

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
         byte[] buffer = new byte[gw * gh];
         for (int i = 0; i < points.Count; i++) {
            var point = points[i];
            var x = ((point.Time.Ticks - minTime.Ticks) * (gw - 1)) / (maxTime.Ticks - minTime.Ticks);
            var y = (int)(gh * (point.Value - minValue) / (maxValue - minValue));
            buffer[(gh - y - 1) * gw + x] = 1;
         }
         
         for (var y = 0; y < gh; y++) {
            Console.SetCursorPosition(renderLeft, renderTop + y + 1);
            var row = Enumerable.Range(0, gw).Select(x => buffer[y * gw + x]).ToArray();
            for (var x = 0; x < gw; x++) {
               Console.Write(row[x] == 0 ? " " : "*");
            }
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
         var statisticsString = $"^: {NumberToLabelString(points.Max(p => p.Value))}, v: {NumberToLabelString(points.Min(p => p.Value))}, μ: {NumberToLabelString(points.Sum(p => p.Value) / points.Count)}";
         Console.Write(statisticsString.PadRight(renderWidth - 1).Substring(0, renderWidth - 1));

         Trace.Assert(Console.CursorTop == renderTop + renderHeight - 1);
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
