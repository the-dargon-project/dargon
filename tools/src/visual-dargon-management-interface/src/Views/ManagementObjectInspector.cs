using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Utilities;
using Dargon.Courier;
using Dargon.Courier.AuditingTier;
using Dargon.Courier.Management.GUI.Views;
using Dargon.Courier.ManagementTier;
using Dargon.Courier.ManagementTier.Vox;
using static Dargon.Courier.ServiceTier.Client.CourierClientRmiStatics;

namespace Views {
   public class ManagementObjectInspector : UserControl {
      private static readonly IGenericFlyweightFactory<Func<ManagementObjectInspector, DataSetDescriptionDto, IChartSeries[]>> createChartSeriesesFuncs = GenericFlyweightFactory.ForStaticMethod<Func<ManagementObjectInspector, DataSetDescriptionDto, IChartSeries[]>>(
         typeof(ManagementObjectInspector),
         nameof(CreateChartSerieses));


      private static IChartSeries[] CreateChartSerieses<T>(ManagementObjectInspector self, DataSetDescriptionDto dsDesc) {
         if (typeof(T).FullName.Contains("Aggregate")) {
            return createAggregateSeriesesFuncs.Get(typeof(T).GenericTypeArguments[0])(self, dsDesc);
         }

         var series = new ChartPointSeries<T>();
         var firstUpdateLatch = new AutoResetEvent(false);

         self.UpdateLoopHelperAsync(async () => {
            var ds = await Async(() => self.vdmiContext.RemoteManagementObjectService.GetManagedDataSet<T>(self.mob.FullName, dsDesc.Name));
            series.DataPoints = ds.DataPoints;
            firstUpdateLatch.Set();
         }).Forget();

         firstUpdateLatch.WaitOne();

         return new IChartSeries[] { series };
      }

      private static readonly IGenericFlyweightFactory<Func<ManagementObjectInspector, DataSetDescriptionDto, IChartSeries[]>> createAggregateSeriesesFuncs = GenericFlyweightFactory.ForStaticMethod<Func<ManagementObjectInspector, DataSetDescriptionDto, IChartSeries[]>>(
         typeof(ManagementObjectInspector),
         nameof(CreateAggregateSerieses));

      private static IChartSeries[] CreateAggregateSerieses<T>(ManagementObjectInspector self, DataSetDescriptionDto dsDesc) {
         var avgSeries = new ChartPointSeries<T>();
         var firstUpdateLatch = new AutoResetEvent(false);

         self.UpdateLoopHelperAsync(async () => {
            var ds = await Async(() => self.vdmiContext.RemoteManagementObjectService.GetManagedDataSet<AggregateStatistics<T>>(self.mob.FullName, dsDesc.Name));
            avgSeries.DataPoints = ds.DataPoints.Map(p => new DataPoint<T> { Time = p.Time, Value = p.Value.Average });
            firstUpdateLatch.Set();
         }).Forget();

         firstUpdateLatch.WaitOne();

         return new IChartSeries[] {
            avgSeries
         };
      }

      private readonly VdmiContext vdmiContext;
      private readonly ManagementObjectIdentifierDto mob;

      public ManagementObjectInspector(VdmiContext vdmiContext, ManagementObjectIdentifierDto mob) {
         this.vdmiContext = vdmiContext;
         this.mob = mob;

         InitializeComponent();
      }

      private void InitializeComponent() {
         AutoScroll = true;
         BackColor = SystemColors.Window;
         Padding = new Padding(10, 10, 10, 10);

         var desc = vdmiContext.RemoteManagementObjectService.GetManagementObjectDescription(mob.Id);

         var outerStack = new StackPanel(FlowDirection.TopDown) { Dock = DockStyle.Top };
         outerStack.Controls.Add(CreateOperationsBoxView(desc));
         outerStack.Controls.Add(CreatePropertiesBoxView(desc));
         outerStack.Controls.Add(CreateDataSetsView(desc));

         Controls.Add(outerStack);
      }

      private UIPaddedAutosizingGroupBox CreateOperationsBoxView(ManagementObjectStateDto desc) {
         var gbox = new UIPaddedAutosizingGroupBox { Text = "Operations" };
         var root = new StackPanel(FlowDirection.TopDown) { Dock = DockStyle.Top };

         foreach (var method in desc.Methods) {
            var methodInvokerView = new MethodInvokerView(method) { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            root.Add(methodInvokerView);

            methodInvokerView.RemoteInvokeRequested += (_, _) => {
               Task.Run(async () => {
                  var args = methodInvokerView.ParameterViews.Map(p => p.ValueEditorView.Value);
                  var result = await vdmiContext.RemoteManagementObjectService.InvokeManagedOperationAsync(mob.FullName, method.Name, args);
                  Console.WriteLine("RMI RESULT " + result.GetType().FullName + ": " + result);
                  MessageBox.Show(result.ToString());
               });
            };
         }

         gbox.Controls.Add(root);
         return gbox;
      }

      private UIPaddedAutosizingGroupBox CreatePropertiesBoxView(ManagementObjectStateDto desc) {
         var gbox = new UIPaddedAutosizingGroupBox { Text = "Properties" };
         var root = new StackPanel(FlowDirection.TopDown) { Dock = DockStyle.Top };

         foreach (var prop in desc.Properties) {
            var propertyEditorView = new PropertyEditorView(prop) { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            root.Add(propertyEditorView);

            SyncProperty(prop, propertyEditorView);
         }

         gbox.Controls.Add(root);
         return gbox;
      }

      internal async Task UpdateLoopHelperAsync(Func<Task> doUpdateAsync) {
         await Task.Delay(1).ConfigureAwait(false);

         var inspector = this;
         while (!inspector.IsDisposed) {
            // if (pendingUpdateOperations.Any()) {
            //    while (pendingUpdateOperations.TryDequeue(out var t)) {
            //       await t;
            //    }
            // }

            var visible = inspector.Visible;

            if (visible) {
               await doUpdateAsync();
            }

            await Task.Delay(DateTime.Now.Millisecond);
         }
      }

      private void SyncProperty(PropertyDescriptionDto prop, PropertyEditorView propertyEditorView) {
         var pendingUpdateOperations = new ConcurrentQueue<Task>();

         ManagementObjectInspector inspector = this;

         UpdateLoopHelperAsync(async () => {
            if (pendingUpdateOperations.Any()) {
               while (pendingUpdateOperations.TryDequeue(out var t)) {
                  await t;
               }
            }
            
            var focused = propertyEditorView.ValueEditorView.HasFocus;

            if (!focused) {
               var latestValue = await vdmiContext.RemoteManagementObjectService.InvokeManagedOperationAsync(mob.FullName, prop.Name, Array.Empty<object>());
               latestValue = CastToTypeIfNecessary(latestValue, prop.Type);
               propertyEditorView.ValueEditorView.Value = latestValue;
            }
         }).Forget();

         propertyEditorView.ValueEditorView.OnValueChanged += (s, e) => {
            var nextValue = propertyEditorView.ValueEditorView.Value; 
            var task = vdmiContext.RemoteManagementObjectService.InvokeManagedOperationAsync(mob.FullName, prop.Name, new[] { nextValue });
            pendingUpdateOperations.Enqueue(task);
         };
      }

      private object CastToTypeIfNecessary(object latestValue, Type propType) {
         if (propType == typeof(int)) {
            return Convert.ToInt32(latestValue);
         }

         return latestValue;
      }

      private Control CreateDataSetsView(ManagementObjectStateDto desc) {
         var gbox = new UIPaddedAutosizingGroupBox { Text = "DataSets", Dock = DockStyle.Top };
         var root = new StackPanel(FlowDirection.TopDown) { Dock = DockStyle.Top };

         // var chart = new ChartView { Dock = DockStyle.Top };
         // root.Add(chart);
         //
         // var autoPanel = new AutoLayoutPanel {
         //    AutoSize = true,
         //    Dock = DockStyle.Top,
         //    CenterHorizontally = true,
         // };
         // root.Add(autoPanel);
         //
         // for (var i = 0; i < 20; i++) {
         //    autoPanel.Controls.Add(new CheckBox { Text = "Asdf", AutoSize = true });
         // }

         foreach (var datasetDesc in desc.DataSets) {
            var box = new UIPaddedAutosizingGroupBox(true) { Text = datasetDesc.Name, Dock = DockStyle.Top };
            var stack = new StackPanel(FlowDirection.TopDown) { Dock = DockStyle.Top };
            var serieses = createChartSeriesesFuncs.Get(datasetDesc.ElementType)(this, datasetDesc);
            var chart = new ChartView { Dock = DockStyle.Top };
            chart.Serieses.AddRange(serieses);
            chart.Invalidate();
            stack.Controls.Add(chart);
            box.Controls.Add(stack);
            root.Add(box);
         }

         gbox.Controls.Add(root);
         return gbox;
      }


   }

   public class UIPaddedAutosizingGroupBox : GroupBox {
      public UIPaddedAutosizingGroupBox(bool padBottom = false) {
         Dock = DockStyle.Top;
         AutoSize = true;
         Padding = new Padding(10, 0, 10, padBottom ? 10 : 2); // at minimum 2 so border is drawn?
      }
   }
}
