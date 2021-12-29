using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dargon.Commons;
using Dargon.Courier;
using Dargon.Courier.Management.GUI.Views;
using Dargon.Courier.ManagementTier;
using Dargon.Courier.PeeringTier;
using Resources;
using WeifenLuo.WinFormsUI.Docking;
using static Dargon.Courier.ServiceTier.Client.CourierClientRmiStatics;

namespace Views {
   public class MainWindow : Form {
      private DockPanel dockPanel;
      private MobsTreeView mobsTreeView;
      private Dictionary<Guid, DockContent> mobGuidToWindow = new();
      private VdmiContext vdmiContext;

      public MainWindow() {
         InitializeComponent();
      }

      private void InitializeComponent() {
         ClientSize = new Size(1024, 1024 * 10 / 16);

         this.StartPosition = FormStartPosition.CenterScreen;
         this.Text = "VDMI: Visual Dargon Management Interface";
         this.Icon = VdmiResources.Dargon64x64Icon;

         this.dockPanel = new DockPanel { Dock = DockStyle.Fill, Theme = new VS2015LightTheme() };
         this.Controls.Add(dockPanel);

         var treeViewPane = new DockContent { Text = "Management Objects Tree", Width = 250};
         treeViewPane.Show(dockPanel, DockState.DockLeft);

         this.mobsTreeView = new MobsTreeView {
            Dock = DockStyle.Fill,
         };
         this.mobsTreeView.ManagementObjectOpened += HandleManagementObjectOpened;
         treeViewPane.Controls.Add(mobsTreeView);

         this.Shown += HandleShown;
      }

      private void HandleShown(object? sender, EventArgs e) {
         var w = new ConnectionWindow();
         w.StartPosition = FormStartPosition.CenterParent;
         w.ShowDialog(this);

         if (w.DialogResult == ConnectionWindow.kCanceledDialogResult) {
            Close();
         }

         mobsTreeView.Focus(); // Keyboard focus starts on mob tree
         InitializeWithCourierAsync(w.CourierFacadeResult).Forget();
      }

      private async Task InitializeWithCourierAsync(CourierFacade courier) {
         var peers = courier.PeerTable.Enumerate().ToArray();
         var remotePeer = peers[0];
         var managementObjectService = courier.RemoteServiceProxyContainer.Get<IManagementObjectService>(remotePeer);
         var mobs = (await Async(() => managementObjectService.EnumerateManagementObjects())).ToArray();
         mobsTreeView.HandleMobsEnumerated(mobs);
         
         this.vdmiContext = new VdmiContext {
            Courier = courier,
            RemotePeer = remotePeer,
            RemoteManagementObjectService = managementObjectService
         };
      }

      private void HandleManagementObjectOpened(object? sender, ManagementObjectIdentifierDto e) {
         if (mobGuidToWindow.TryGetValue(e.Id, out var existingWindow)) {
            existingWindow.Activate();
            
            if (existingWindow.DockState == DockState.Float) {
               // existingWindow.BringToFront();
            }
            return;
         }

         var window = new DockContent { Text = e.FullName.Split(".").Last() };
         var inspector = new ManagementObjectInspector(vdmiContext, e) { Dock = DockStyle.Fill };
         window.Controls.Add(inspector);
         window.Show(dockPanel, DockState.Document);
         mobGuidToWindow.Add(e.Id, window);

         window.Closed += (s, _) => mobGuidToWindow.Remove(e.Id);
      }
   }

   public class VdmiContext {
      public CourierFacade Courier;
      public PeerContext RemotePeer;
      public IManagementObjectService RemoteManagementObjectService;
   }
}
