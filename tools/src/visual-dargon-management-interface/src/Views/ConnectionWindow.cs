using System;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using Dargon.Commons;
using Dargon.Courier.TransportTier.Tcp;
using Dargon.Courier.TransportTier.Tcp.Server;
using Dargon.Courier.Utils;
using Dargon.Ryu;
using Dargon.Vox.Ryu;
using Resources;
using Views;

namespace Dargon.Courier.Management.GUI.Views {
   public class ConnectionWindow : Form {
      public const DialogResult kSucceededDialogResult = DialogResult.OK;
      public const DialogResult kCanceledDialogResult = DialogResult.Cancel;

      public ConnectionWindow() {
         InitializeComponents();
         StartPosition = FormStartPosition.CenterScreen;
      }
      
      public CourierFacade CourierFacadeResult { get; set; }

      private void InitializeComponents() {
         FormBorderStyle = FormBorderStyle.FixedDialog;
         MaximizeBox = false;
         MinimizeBox = false;
         Margin = Padding = Padding.Empty;
         
         var root = new StackPanel(FlowDirection.TopDown) {
            Anchor = AnchorStyles.Left | AnchorStyles.Top
         };
         Controls.Add(root);

         var headerWrapper = new StackPanel(FlowDirection.TopDown) {
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.White
         };
         root.Add(headerWrapper);

         var header = new StackPanel(FlowDirection.LeftToRight) {
            Padding = new Padding(20, 15, 20, 15)
         };
         headerWrapper.Add(header);

         header.Add(new PictureBox {
            Image = VdmiResources.Dargon64x64Bitmap,
            Size = new Size(64, 64),
            Margin = Padding.Empty,
            Padding = Padding.Empty
         });
         header.Add(new UILabel(true) {
            Text = "New Connection",
            Font = new Font(FontFamily.GenericSansSerif, 15.75f, FontStyle.Regular, GraphicsUnit.Point),
            Anchor = AnchorStyles.Left,
            Margin = new Padding(10, 12, 3, 0)
         });

         var optionPanelsContainer = new StackPanel(FlowDirection.TopDown) {
            Margin = new Padding(15, 15, 15, 15),
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom
         };
         root.Add(optionPanelsContainer);

         var optionsRadioGroup = new RadioButtonGroup();

         var connectToHostPanel = new ConnectToHostPanel();
         optionsRadioGroup.Add(connectToHostPanel.RadioButton);

            //CreateConnectToHostPanel(optionsRadioGroup, out var hostInputTextBox);
         connectToHostPanel.Margin = new Padding(0, 0, 0, 3);
         optionPanelsContainer.Add(connectToHostPanel);

         var connectByDiscoveryPanel = CreateConnectByDiscoveryPanel(optionsRadioGroup);
         connectByDiscoveryPanel.Margin = new Padding(0, 0, 0, 3);
         // optionPanelsContainer.Add(connectByDiscoveryPanel);

         Button connectButton, cancelButton;
         var connectCancelPanel = CreateConnectCancelPanel(out connectButton, out cancelButton);
         optionPanelsContainer.Add(connectCancelPanel);
         UiUtils.ProxyFormKeyEvents(connectToHostPanel, connectButton, cancelButton);
         UiUtils.ProxyFormKeyEvents(connectByDiscoveryPanel, connectButton, cancelButton);
         UiUtils.ProxyFormKeyEvents(connectButton, connectButton, cancelButton);
         UiUtils.ProxyFormKeyEvents(cancelButton, connectButton, cancelButton);
         connectButton.Click += (s, e) => {
            Console.WriteLine("Connect");
            connectToHostPanel.HideErrorText();
            if (connectToHostPanel.RadioButton.Checked) {
               TryConnect(() => ProcessConnectToHost(connectToHostPanel));
            }
         };
         cancelButton.Click += (s, e) => {
            Console.WriteLine("Cancel");
            DialogResult = DialogResult.Cancel;
            Close();
         };

         Shown += HandleResize;

         // Force initial width to ensure golden aspect ratio
         // ClientSize = root.Size;
         this.AutoSize = true;
         this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
         int desiredWidth = (int)(Height * 1.61803398875);
         int actualWidth = Width;
         headerWrapper.MinimumSize = new Size(root.ClientSize.Width + (desiredWidth - actualWidth), 0);
         ClientSize = root.Size;
      }

      private void TryConnect(Func<CourierFacade> cb) {
         try {
            Enabled = false;
            CourierFacadeResult = cb();
            DialogResult = kSucceededDialogResult;
         } catch (Exception e) {
            Console.WriteLine("Connect failure: " + e);
         } finally {
            Enabled = true;
         }
      }

      private CourierFacade ProcessConnectToHost(ConnectToHostPanel connectToHostPanel) {
         var connectionString = connectToHostPanel.GetConnectionString();

         const string kProtocolTcp = "tcp";
         const string kProtocolUdp = "ucp";
         const string kProtocolDelimiter = "://";

         var selectedProtocol = kProtocolTcp;
         var hostport = connectionString;
         foreach (var candidate in new[] { kProtocolTcp, kProtocolUdp }) {
            var protocolAndDelimiter = candidate + kProtocolDelimiter;
            if (connectionString.StartsWith(protocolAndDelimiter, StringComparison.InvariantCultureIgnoreCase)) {
               selectedProtocol = candidate;
               hostport = connectionString.Substring(protocolAndDelimiter.Length);
            }
         }

         if (!hostport.Contains(":")) {
            hostport += ":21337";
         }

         if (!IPEndPointUtils.TryParseIpEndpoint(hostport, out var endpoint)) {
            throw new ArgumentException("Failed to parse or resolve IP Endpoint: " + endpoint);
         }

         var completionLatch = new ManualResetEvent(false);
         var completionError = (Exception)null;
         var tcpTransportConfiguration = new TcpTransportConfiguration(endpoint, TcpRole.Client);
         tcpTransportConfiguration.ConnectionFailure += e => {
            completionError = e.Exception;
            completionLatch.Set();
         };
         tcpTransportConfiguration.HandshakeCompleted += e => {
            completionLatch.Set();
         };

         var ryuConfig = new RyuConfiguration {
            AdditionalModules = {
               new CourierRyuModule(),
               new VoxRyuExtensionModule(),
            }
         };
         var ryu = new RyuFactory().Create(ryuConfig);
         var courierFacade = CourierBuilder.Create(ryu)
                                           .UseTransport(new TcpTransportFactory(tcpTransportConfiguration))
                                           .BuildAsync()
                                           .Result;

         completionLatch.WaitOne();

         if (completionError != null) {
            connectToHostPanel.SetErrorText(completionError.ToString());
            courierFacade.ShutdownAsync().Forget();
            throw completionError;
         }

         Console.WriteLine("Success? " + completionError);
         return courierFacade;
      }

      private StackPanel CreateConnectByDiscoveryPanel(RadioButtonGroup optionsRadioGroup) {
         var root = new StackPanel(FlowDirection.LeftToRight) {
            Anchor = AnchorStyles.Left | AnchorStyles.Right
         };

         var radio = new UntabbableRadioButton {
            Margin = Padding.Empty,
            Padding = new Padding(0, 2, 5, 0),
            AutoSize = true
         };
         optionsRadioGroup.Add(radio);
         root.Add(radio);

         var body = new StackPanel(FlowDirection.TopDown) {
            Anchor = AnchorStyles.Left | AnchorStyles.Right
         };
         root.Add(body, true);

         var bodyLabel = new UILabel {
            Text = "Connect to Discovered Node:",
            Anchor = AnchorStyles.Left,
            Font = new Font(FontFamily.GenericSansSerif, 8.75f, FontStyle.Bold, GraphicsUnit.Point)
         };
         body.Add(bodyLabel);

         var inputListBox = new ListBox {
            Margin = new Padding(0, 0, 0, 2),
            Anchor = AnchorStyles.Left | AnchorStyles.Right
         };
         body.Add(inputListBox);

         var errorlabelPanel = new LabeledTextPanel {
            ForeColor = Color.FromArgb(192, 0, 0),
            Label = "Error:",
            Text = "asdf<hostname>:<port>\r\nHello\r\nDSFJOI"
         };
         errorlabelPanel.Hide();
         body.Add(errorlabelPanel);

         UiUtils.ProxyFocusToCheck(body, radio);
         return root;
      }

      private StackPanel CreateConnectCancelPanel(out Button connectButton, out Button cancelButton) {
         var root = new StackPanel(FlowDirection.LeftToRight) {
            Anchor = AnchorStyles.Right
         };

         root.Add(connectButton = new Button {
            Text = "Connect",
            Margin = new Padding(0, 0, 5, 0),
            Padding = Padding.Empty
         });
         root.Add(cancelButton = new Button {
            Text = "Cancel",
            Margin = Padding.Empty,
            Padding = Padding.Empty
         });

         root.Margin = new Padding(0, 5, 0, 0);

         return root;
      }

      private void HandleResize(object sender, EventArgs e) {
         MinimumSize = new Size(1, 1);

//         BackColor = Color.LightGray;
      }
   }

   public class LabeledTextPanel : StackPanel {
      private UILabel label, desc;
      private Color foreColor = SystemColors.ControlText;

      public LabeledTextPanel() : base(FlowDirection.LeftToRight) {
         base.Anchor = AnchorStyles.Left;

         Add(label = new UILabel(true) {
            Anchor = AnchorStyles.Top,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = foreColor,
         });
         Add(desc = new UILabel(true) {
            Anchor = AnchorStyles.Top,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = foreColor,
         });
      }

      public string Label {
         get => label.Text;
         set => label.Text = value;
      }

      public override string Text {
         get => desc.Text;
         set => desc.Text = value;
      }

      public override Color ForeColor {
         get => foreColor;
         set => label.ForeColor = desc.ForeColor = foreColor = value;
      }
   }

   public static class UiUtils {
      public static void ProxyFocusToCheck(Control source, RadioButton target) {
         source.GotFocus += (s, e) => {
            target.Checked = true;
         };
         source.MouseDown += (s, e) => {
            target.Checked = true;
         };
         foreach (Control child in source.Controls) {
            ProxyFocusToCheck(child, target);
         }
      }

      public static void ProxyFormKeyEvents(Control source, Button submit, Button cancel) {
         source.KeyPress += (s, e) => {
            const int kEscapeAscii = 27;
            if ("\r\n".Contains(e.KeyChar + "") && source != submit && source != cancel) {
               submit.PerformClick();
               e.Handled = true;
            } else if (e.KeyChar == kEscapeAscii) {
               cancel.PerformClick();
               e.Handled = true;
            }
         };
         foreach (Control child in source.Controls) {
            ProxyFormKeyEvents(child, submit, cancel);
         }
      }

      public static Size GetTextSize(string text, Font font) {
         // referenced http://stackoverflow.com/questions/21632642/label-without-padding-and-margin
         // though it leaves a big gap on the right, so I ended up hacking things anyway
         var lines = text.Split("\n").Map(line => line.Trim('\r'));
         Size horizontalPadSize1 = TextRenderer.MeasureText(".", font);
         Size horizontalPadSize2 = TextRenderer.MeasureText("...", font);
         var widthCut = (horizontalPadSize2.Width - horizontalPadSize1.Width) * 8 / 10; // all hacky and wrong
         Size verticalPadSize = TextRenderer.MeasureText(".", font);
         Size textSize = TextRenderer.MeasureText(lines.Join(".\r\n") + "\n.", font);
         return new Size(textSize.Width - widthCut, textSize.Height - verticalPadSize.Height);
      }
   }
}
