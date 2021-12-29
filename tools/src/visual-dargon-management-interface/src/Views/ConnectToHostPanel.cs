using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dargon.Courier.Management.GUI.Views;

namespace Views {
   public class ConnectToHostPanel : StackPanel {
      private TextBox connectionStringTextBox;
      private LabeledTextPanel errorlabelPanel;

      public ConnectToHostPanel() : base(FlowDirection.LeftToRight) {
         InitializeComponent();
      }

      public UntabbableRadioButton RadioButton { get; private set; }

      public string GetConnectionString() {
         return connectionStringTextBox.Text;
      }

      public void HideErrorText() {
         errorlabelPanel.Hide();
      }

      public void SetErrorText(string s) {
         errorlabelPanel.Text = s;
         errorlabelPanel.Show();
      }

      private void InitializeComponent() {
         Anchor = AnchorStyles.Left | AnchorStyles.Right;
         Margin = Padding = Padding.Empty;

         var radio = RadioButton = new UntabbableRadioButton {
            Margin = Padding.Empty,
            Padding = new Padding(0, 2, 5, 0),
            AutoSize = true
         };
         Add(radio);

         var body = new StackPanel(FlowDirection.TopDown) {
            Anchor = AnchorStyles.Left | AnchorStyles.Right
         };
         Add(body, true);

         var bodyLabel = new UILabel {
            Text = "Connect to Host:",
            Anchor = AnchorStyles.Left,
            Font = new Font(FontFamily.GenericSansSerif, 8.75f, FontStyle.Bold, GraphicsUnit.Point)
         };
         body.Add(bodyLabel);

         var inputTextBox = this.connectionStringTextBox = new TextBox {
            Margin = new Padding(0, 0, 0, 2),
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Text = "127.0.0.1:21337",
         };
         body.Add(inputTextBox);

         body.Add(new LabeledTextPanel {
            ForeColor = SystemColors.GrayText,
            Label = "Usage:",
            Text = "[tcp|udp]://<hostname>:<port>",
         });

         this.errorlabelPanel = new LabeledTextPanel {
            ForeColor = Color.FromArgb(192, 0, 0),
            Label = "Error:",
            Text = "asdf<hostname>:<port>\r\nHello\r\nDSFJOI"
         };
         errorlabelPanel.Hide();
         body.Add(errorlabelPanel);

         UiUtils.ProxyFocusToCheck(body, radio);
      }

   }
}
