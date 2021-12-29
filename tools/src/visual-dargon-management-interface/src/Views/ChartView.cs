using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dargon.Commons;
using Dargon.Courier.AuditingTier;
using Dargon.Courier.ManagementTier.Vox;

namespace Views {
   public delegate double ChartViewPercentComputer<T>(T val, T min, T maxMinusMin);

   public interface IChartSeries {
      public event EventHandler Updated;
      Vector2[] NormalizeForAxis(ChartPointAxis<DateTime> taxis);
   }

   public class ChartPointAxis<T> {
      public string Name;
      public bool Normalize;
      public T Min;
      public T Max;
   }

   public class ChartPointSeries<T> : IChartSeries {
      private static Func<T, T, T> s_subtractFunc;
      private static ChartViewPercentComputer<T> s_percentFunc;

      static ChartPointSeries() {
         if (typeof(T) == typeof(bool)) {
            s_subtractFunc = (Func<T, T, T>)(object)new Func<bool, bool, bool>((a, b) => a != b);
            s_percentFunc = (ChartViewPercentComputer<T>)(object)new ChartViewPercentComputer<bool>((val, min, range) => val ? 1 : 0);
         } else if (typeof(T) == typeof(byte)) {
            s_subtractFunc = (Func<T, T, T>)(object)new Func<byte, byte, byte>((a, b) => (byte)(a - b));
            s_percentFunc = (ChartViewPercentComputer<T>)(object)new ChartViewPercentComputer<byte>((val, min, range) => (val - min) / (double)range);
         } else if (typeof(T) == typeof(short)) {
            s_subtractFunc = (Func<T, T, T>)(object)new Func<short, short, short>((a, b) => (short)(a - b));
            s_percentFunc = (ChartViewPercentComputer<T>)(object)new ChartViewPercentComputer<short>((val, min, range) => (val - min) / (double)range);
         } else if (typeof(T) == typeof(int)) {
            s_subtractFunc = (Func<T, T, T>)(object)new Func<int, int, int>((a, b) => a - b);
            s_percentFunc = (ChartViewPercentComputer<T>)(object)new ChartViewPercentComputer<int>((val, min, range) => (val - min) / (double)range);
         } else if (typeof(T) == typeof(long)) {
            s_subtractFunc = (Func<T, T, T>)(object)new Func<long, long, long>((a, b) => a - b);
            s_percentFunc = (ChartViewPercentComputer<T>)(object)new ChartViewPercentComputer<long>((val, min, range) => (val - min) / (double)range);
         } else if (typeof(T) == typeof(float)) {
            s_subtractFunc = (Func<T, T, T>)(object)new Func<float, float, float>((a, b) => a - b);
            s_percentFunc = (ChartViewPercentComputer<T>)(object)new ChartViewPercentComputer<float>((val, min, range) => (val - min) / (double)range);
         } else if (typeof(T) == typeof(double)) {
            s_subtractFunc = (Func<T, T, T>)(object)new Func<double, double, double>((a, b) => a - b);
            s_percentFunc = (ChartViewPercentComputer<T>)(object)new ChartViewPercentComputer<double>((val, min, range) => (val - min) / range);
         }
      }

      private DataPoint<T>[] dataPointsInternal;
      public DataPoint<T>[] DataPoints {
         get => dataPointsInternal;
         set {
            dataPointsInternal = value;
            Updated?.Invoke(this, EventArgs.Empty);
         } }
      public event EventHandler Updated;

      public Vector2[] NormalizeForAxis(ChartPointAxis<DateTime> taxis) {
         return NormalizeForAxis(taxis, new ChartPointAxis<T> { Normalize = true });
      }

      public Vector2[] NormalizeForAxis(ChartPointAxis<DateTime> taxis, ChartPointAxis<T> vaxis) {
         var ps = DataPoints;
         if (ps.Length == 0) {
            return Array.Empty<Vector2>();
         }

         var minT = taxis.Normalize ? ps.Min(x => x.Time) : taxis.Min;
         var maxT = taxis.Normalize ? ps.Max(x => x.Time) : taxis.Max;
         var timeInterval = maxT - minT;

         var minV = vaxis.Normalize ? ps.Min(x => x.Value) : vaxis.Min;
         var maxV = vaxis.Normalize ? ps.Max(x => x.Value) : vaxis.Max;
         var rangeInterval = s_subtractFunc(maxV, minV);

         return ps.Map(p => {
            var x = (float)((p.Time - minT) / timeInterval);
            var y = (float)s_percentFunc(p.Value, minV, rangeInterval);
            return new Vector2(x, y);
         });
      }
   }

   public class ChartView : UserControl {
      public ChartView() {
         SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

         Serieses.CollectionChanged += (_, e) => {
            Invalidate();

            if (e.OldItems != null) {
               foreach (IChartSeries s in e.OldItems) {
                  s.Updated -= HandleSeriesUpdated;
               }
            }

            if (e.NewItems != null) {
               foreach (IChartSeries s in e.NewItems) {
                  s.Updated += HandleSeriesUpdated;
               }
            }
         };
      }

      private void HandleSeriesUpdated(object? sender, EventArgs e) {
         Invalidate();
      }

      public ObservableCollection<IChartSeries> Serieses { get; } = new ObservableCollection<IChartSeries>();

      protected override void OnPaint(PaintEventArgs e) {
         e.Graphics.Clear(Color.White);

         var cs = ClientSize;
         var padding = 5;
         
         e.Graphics.DrawLine(Pens.Black, padding, cs.Height - padding, cs.Width - padding, cs.Height - padding);
         e.Graphics.DrawLine(Pens.Black, padding, padding, padding, cs.Height - padding);

         var innerSize = new Size(cs.Width - padding * 2, cs.Height - padding * 2);

         var now = DateTime.Now;
         var past = now - TimeSpan.FromMinutes(5);
         foreach (var series in Serieses) {
            var normalizedPoints = series.NormalizeForAxis(new ChartPointAxis<DateTime> {
               Min = past,
               Max = now,
               Normalize = false
            });
            var ps = normalizedPoints.Where(p => p.X >= 0 && p.X <= 1 && p.Y >= 0 && p.Y <= 1)
                                     .Select(p => new RectangleF(p.X * innerSize.Width + padding - 1, (1 - p.Y) * innerSize.Height + padding - 1, 3, 3))
                                     .ToArray();
            if (ps.Length > 0) {
               e.Graphics.FillRectangles(Brushes.Cyan, ps);
            }
         }
      }
   }
}
