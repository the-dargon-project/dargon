using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Dargon.Courier.Management.GUI.Views {
   public class ConnectionWindow : Form {
      public ConnectionWindow() {
         InitializeComponents();
         StartPosition = FormStartPosition.CenterScreen;
      }

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
            Image = Image.FromFile("Resources/Dargon6464.png"),
            Size = new Size(64, 64),
            Margin = Padding.Empty,
            Padding = Padding.Empty
         });
         header.Add(new ResetLabel(true) {
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

         var connectToHostPanel = CreateConnectToHostPanel(optionsRadioGroup);
         connectToHostPanel.Margin = new Padding(0, 0, 0, 3);
         optionPanelsContainer.Add(connectToHostPanel);

         var connectByDiscoveryPanel = CreateConnectByDiscoveryPanel(optionsRadioGroup);
         connectByDiscoveryPanel.Margin = new Padding(0, 0, 0, 3);
//         optionPanelsContainer.Add(connectByDiscoveryPanel);

         Button connectButton, cancelButton;
         var connectCancelPanel = CreateConnectCancelPanel(out connectButton, out cancelButton);
         optionPanelsContainer.Add(connectCancelPanel);
         ProxyFormKeyEvents(connectToHostPanel, connectButton, cancelButton);
         ProxyFormKeyEvents(connectByDiscoveryPanel, connectButton, cancelButton);
         ProxyFormKeyEvents(connectButton, connectButton, cancelButton);
         ProxyFormKeyEvents(cancelButton, connectButton, cancelButton);
         connectButton.Click += (s, e) => {
            Console.WriteLine("Connect");
         };
         cancelButton.Click += (s, e) => {
            Console.WriteLine("Cancel");
         };

         Shown += HandleResize;

         // Force initial width to ensure golden aspect ratio
         ClientSize = root.Size;
         int desiredWidth = (int)(Height * 1.61803398875);
         int actualWidth = Width;
         headerWrapper.MinimumSize = new Size(root.ClientSize.Width + (desiredWidth - actualWidth), 0);
         ClientSize = root.Size;
      }

      private StackPanel CreateConnectToHostPanel(RadioButtonGroup optionsRadioGroup) {
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

         var bodyLabel = new ResetLabel {
            Text = "Connect to Host:",
            Anchor = AnchorStyles.Left,
            Font = new Font(FontFamily.GenericSansSerif, 8.75f, FontStyle.Bold, GraphicsUnit.Point)
         };
         body.Add(bodyLabel);

         var inputTextBox = new TextBox {
            Margin = new Padding(0, 0, 0, 2),
            Anchor = AnchorStyles.Left | AnchorStyles.Right
         };
         body.Add(inputTextBox);

         Label usageLabel;
         body.Add(CreateLabeledLabelPanel("Usage:", SystemColors.GrayText, out usageLabel));
         usageLabel.Text = "<hostname>:<port>";

         Label errorLabel;
         var errorlabelPanel = CreateLabeledLabelPanel("Error:", Color.FromArgb(192, 0, 0), out errorLabel);
         errorLabel.Text = "asdf<hostname>:<port>\r\nHello\r\nDSFJOI";
         errorlabelPanel.Hide();
         body.Add(errorlabelPanel);

         ProxySelection(body, radio);

         return root;
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

         var bodyLabel = new ResetLabel {
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

         Label errorLabel;
         var errorlabelPanel = CreateLabeledLabelPanel("Error:", Color.FromArgb(192, 0, 0), out errorLabel);
         errorLabel.Text = "asdf<hostname>:<port>\r\nHello\r\nDSFJOI";
         errorlabelPanel.Hide();
         body.Add(errorlabelPanel);

         ProxySelection(body, radio);

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

         return root;
      }

      private StackPanel CreateLabeledLabelPanel(string label, Color foreColor, out Label labeledLabel) {
         var root = new StackPanel(FlowDirection.LeftToRight) { Anchor = AnchorStyles.Left };

         root.Add(new ResetLabel(true) {
            Text = label,
            Anchor = AnchorStyles.Top,
            ForeColor = foreColor,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold, GraphicsUnit.Point)
         });
         root.Add(labeledLabel = new ResetLabel(true) {
            Anchor = AnchorStyles.Top,
            ForeColor = foreColor,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Regular, GraphicsUnit.Point)
         });

         return root;
      }

      private void ProxySelection(Control source, RadioButton target) {
         source.GotFocus += (s, e) => {
            target.Checked = true;
         };
         source.MouseDown += (s, e) => {
            target.Checked = true;
         };
         foreach (Control child in source.Controls) {
            ProxySelection(child, target);
         }
      }

      private void ProxyFormKeyEvents(Control source, Button submit, Button cancel) {
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

      private void HandleResize(object sender, EventArgs e) {
         MinimumSize = new Size(1, 1);

//         BackColor = Color.LightGray;
      }
   }

   public class UntabbableRadioButton : RadioButton {
      public UntabbableRadioButton() {
         TabStop = false;
      }

      protected override void OnCheckedChanged(EventArgs e) {
         base.OnCheckedChanged(e);

         TabStop = false;
      }
   }

   public class RadioButtonGroup {
      private readonly List<RadioButton> buttons = new List<RadioButton>();

      public void Add(RadioButton button) {
         buttons.Add(button);

         button.CheckedChanged += (s, e) => {
            if (button.Checked) {
               HandleButtonChecked(button);
            }
         };

         if (button.Checked) {
            HandleButtonChecked(button);
         }
      }

      private void HandleButtonChecked(RadioButton source) {
         foreach (var button in buttons) {
            button.Checked = button == source;
         }
      }
   }

   public class ResetLabel : Label {
      private readonly bool trim;

      public ResetLabel(bool trim = false) {
         this.trim = trim;
         AutoSize = !trim;
         Anchor = AnchorStyles.None;
         Margin = Padding = Padding.Empty;
         BorderStyle = BorderStyle.None;
         FlatStyle = FlatStyle.System;
      }

      protected override void OnFontChanged(EventArgs e) {
         base.OnFontChanged(e);

         if (trim) {
            Size = GetTextSize();
         }
      }

      protected override void OnResize(EventArgs e) {
         base.OnResize(e);
         if (trim) {
            Size = GetTextSize();
         }
      }

      protected override void OnTextChanged(EventArgs e) {
         base.OnTextChanged(e);
         if (trim) {
            Size = GetTextSize();
         }
      }

      private Size GetTextSize() {
         // http://stackoverflow.com/questions/21632642/label-without-padding-and-margin
         Size padSize = TextRenderer.MeasureText(".", Font);
         Size textSize = TextRenderer.MeasureText(Text + "\r\n.", Font);
         return new Size(textSize.Width, textSize.Height - padSize.Height);
      }
   }

   public class StackPanel : TableLayoutPanel {
      private readonly bool isVertical;
      private int next;

      public StackPanel(FlowDirection direction) {
         Trace.Assert(direction == FlowDirection.LeftToRight || direction == FlowDirection.TopDown);
         isVertical = direction == FlowDirection.TopDown;

         Anchor = AnchorStyles.None;
         AutoSize = true;
         AutoSizeMode = AutoSizeMode.GrowAndShrink;
         Padding = Padding.Empty;
         Margin = Padding.Empty;
      }

      public void Add(Control control, bool fill = false) {
         var sizeType = fill ? SizeType.Percent : SizeType.AutoSize;
         if (isVertical) {
            Controls.Add(control, 0, next);

            RowStyles.Add(new RowStyle {
               Height = 100.0f,
               SizeType = sizeType
            });

            Trace.Assert(RowStyles.Count == next + 1);
         } else {
            
            Controls.Add(control, next, 0);

            ColumnStyles.Add(new ColumnStyle {
               Width = 100.0f,
               SizeType = sizeType
            });
         }
         next++;
      }
   }
}
