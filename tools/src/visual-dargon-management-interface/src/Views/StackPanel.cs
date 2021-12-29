using System.Diagnostics;
using System.Windows.Forms;

namespace Dargon.Courier.Management.GUI.Views {
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
               Height = 1.0f, // doesn't matter?
               SizeType = sizeType
            });

            Trace.Assert(RowStyles.Count == next + 1);
         } else {
            
            Controls.Add(control, next, 0);

            ColumnStyles.Add(new ColumnStyle {
               Width = 1.0f, // doesn't matter?
               SizeType = sizeType
            });
         }
         next++;
      }
   }
}