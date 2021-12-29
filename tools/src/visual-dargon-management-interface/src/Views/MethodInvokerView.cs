using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dargon.Commons;
using Dargon.Courier.Management.GUI.Views;
using Dargon.Courier.ManagementTier;
using Dargon.Courier.ManagementTier.Vox;
using Microsoft.VisualBasic.ApplicationServices;

namespace Views {
   public class MethodInvokerView : UserControl {
      private readonly MethodDescriptionDto methodDescriptionDto;

      public MethodInvokerView(MethodDescriptionDto methodDescriptionDto) {
         this.methodDescriptionDto = methodDescriptionDto;
         InitializeComponent();
      }

      public event EventHandler RemoteInvokeRequested;

      public MethodParameterView[] ParameterViews { get; private set; }

      private void InitializeComponent() {
         AutoSize = true;
         Margin = default;
         Padding = default;

         var stack = new StackPanel(FlowDirection.LeftToRight) { Dock = DockStyle.Fill};
         stack.Add(new UILabel(true) { Text = methodDescriptionDto.Name });
         stack.Add(new UILabel(true) { Text = "(" });

         ParameterViews = methodDescriptionDto.Parameters.Map(p => new MethodParameterView(p));
         foreach (var pv in ParameterViews) stack.Add(pv);

         stack.Add(new UILabel(true) { Text = ")" });
         
         var invokeButton = new Button { Text = "Invoke" };
         var resetButton = new Button { Text = "Reset" };
         stack.Add(invokeButton);
         Controls.Add(stack);

         invokeButton.Click += (_, _) => RemoteInvokeRequested?.Invoke(this, EventArgs.Empty);

         UiUtils.ProxyFormKeyEvents(stack, invokeButton, resetButton);
      }
   }

   public class MethodParameterView : StackPanel {
      public MethodParameterView(ParameterDescriptionDto parameterDescriptionDto) : base(FlowDirection.LeftToRight) {
         Add(new UILabel (true) {
            Text = parameterDescriptionDto.Name,
         });

         ValueEditorView = ValueEditorViewFactory.CreateForType(parameterDescriptionDto.Type, false);
         Add(ValueEditorView.Control);
      }

      public IValueEditorView ValueEditorView;
   }

   public class PropertyEditorView : StackPanel {
      private readonly PropertyDescriptionDto propertyDescriptionDto;

      public IValueEditorView ValueEditorView;

      public PropertyEditorView(PropertyDescriptionDto propertyDescriptionDto) : base(FlowDirection.LeftToRight) {
         this.propertyDescriptionDto = propertyDescriptionDto;

         base.Text = propertyDescriptionDto.Name;
         Margin = new Padding(0, 0, 0, 10);

         Add(new UILabel(true) { Text = propertyDescriptionDto.Name, Anchor = AnchorStyles.Left});
         Add(new Panel { Width = 1, Height = 1, Anchor = AnchorStyles.Left | AnchorStyles.Right, BackColor = SystemColors.ControlLight }, true);
         var valueEditor = ValueEditorView = ValueEditorViewFactory.CreateForType(propertyDescriptionDto.Type, !propertyDescriptionDto.HasSetter);
         valueEditor.Control.Dock = DockStyle.Right;
         Add(valueEditor.Control);
      }
   }

   public interface IValueEditorView {
      public Control Control { get; }
      public object Value { get; set; }
      bool HasFocus { get; }
      public event EventHandler OnValueChanged; // When the input value changes meaningfully. Triggers a property update.
   }
   public interface IValueEditorView<T> : IValueEditorView {
      public new T Value { get; set; }
   }

   public static class ValueEditorViewFactory {
      public static IValueEditorView CreateForType(Type t, bool isReadOnly) {
         if (t == typeof(string)) {
            return new StringValueEditorView(isReadOnly);
         } else if (t == typeof(int)) {
            return new Int32ValueEditorView(isReadOnly);
         } else if (t == typeof(bool)) {
            return new BoolValueEditorView(isReadOnly);
         } else {
            return (IValueEditorView)Activator.CreateInstance(typeof(UnsupportedValueEditorView<>).MakeGenericType(t));
         }
      }
   }

   public class UnsupportedValueEditorView<T> : UserControl, IValueEditorView<T> {
      public event EventHandler OnValueChanged;

      public Control Control => this;

      public T Value { get; set; }

      object IValueEditorView.Value {
         get => Value;
         set => Value = (T)value;
      }

      public bool HasFocus { get; }
   }

   public class StringValueEditorView : StackPanel, IValueEditorView<string> {
      private readonly TextBox textBox;
      private bool suppressInternalChangeHandlers = false;

      public StringValueEditorView(bool isReadOnly) : base(FlowDirection.LeftToRight) {
         Enabled = !isReadOnly;
         textBox = new TextBox { Dock = DockStyle.Fill };
         Add(textBox);

         textBox.KeyUp += (_, e) => OnValueChanged?.Invoke(this, EventArgs.Empty);
      }

      public event EventHandler OnValueChanged;

      public Control Control => this;

      public string Value {
         get => textBox.Text;
         set {
            suppressInternalChangeHandlers = true;
            textBox.Text = value;
            suppressInternalChangeHandlers = false;
         }
      }

      object IValueEditorView.Value
      {
         get => Value;
         set => Value = (string)value;
      }

      public bool HasFocus => textBox.Focused;
   }

   public class UnpaddedTrackBarWrapper : UserControl {
      public UnpaddedTrackBarWrapper() {
         Trackbar = new TrackBar { Left = 0, Top = 0 };
         Controls.Add(Trackbar);

         Width = 100;
      }

      public new int Width {
         get => base.Width;
         set {
            base.Width = value;
            Trackbar.Width = value;
         }
      }

      public TrackBar Trackbar { get; }
   }

   public class Int32ValueEditorView : StackPanel, IValueEditorView<int> {
      private readonly NumericUpDown numericUpDown;
      private bool suppressInternalChangeHandlers = false;

      public Int32ValueEditorView(bool isReadOnly) : base(FlowDirection.LeftToRight) {
         Enabled = !isReadOnly;

         // var trackbarWrapper = new UnpaddedTrackBarWrapper { Width = 200, Height = 30, BackColor = Color.Yellow, Trackbar = { BackColor = Color.Orange } };
         numericUpDown = new NumericUpDown { Width = 80, Increment = 1, Minimum = int.MinValue, Maximum = int.MaxValue };
         // trackbarWrapper.Height = numericUpDown.Height;
         // Add(trackbarWrapper);
         Add(numericUpDown);

         // The nice thing about ValueChanged is it only fires when input is done, not between number strokes.
         numericUpDown.ValueChanged += HandleNumericUpDownValueChanged;
      }

      private void HandleNumericUpDownValueChanged(object? sender, EventArgs e) {
         if (suppressInternalChangeHandlers) return;

         suppressInternalChangeHandlers = true;

         // todo: trackbar sync

         suppressInternalChangeHandlers = false;

         OnValueChanged?.Invoke(this, EventArgs.Empty);
      }


      public event EventHandler OnValueChanged;

      public Control Control => this;

      public int Value {
         get => (int)numericUpDown.Value;
         set {
            suppressInternalChangeHandlers = true;
            numericUpDown.Value = value;
            suppressInternalChangeHandlers = false;
         }
      }

      object IValueEditorView.Value
      {
         get => Value;
         set => Value = (int)value;
      }

      /// <summary>
      /// NumericUpDown.Focus and ContainsFocus doesn't work, but when it's selected it has an active child control, so that works.
      /// </summary>
      public bool HasFocus => numericUpDown.ActiveControl != null;
   }

   public class BoolValueEditorView : StackPanel, IValueEditorView<bool> {
      private CheckBox checkBox;
      private bool suppressInternalChangeHandlers = false;

      public BoolValueEditorView(bool isReadOnly) : base(FlowDirection.LeftToRight) {
         Enabled = !isReadOnly;

         checkBox = new CheckBox { AutoSize = true };
         Add(checkBox);

         checkBox.CheckedChanged += (_, _) => {
            if (suppressInternalChangeHandlers) return;
            OnValueChanged?.Invoke(this, EventArgs.Empty);
         };
      }

      public event EventHandler OnValueChanged;

      public Control Control => this;

      public bool Value {
         get => checkBox.Checked;
         set {
            suppressInternalChangeHandlers = true;
            checkBox.Checked = value;
            suppressInternalChangeHandlers = false;
         }
      }

      object IValueEditorView.Value
      {
         get => Value;
         set => Value = (bool)value;
      }

      public bool HasFocus => checkBox.Focused;
   }
}
