using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dargon.Commons.Collections;

namespace Views {
   public class AutoLayoutPanel : Panel {
      public AutoLayoutPanel() { }

      public int HorizontalSpacing { get; set; } = 30;
      public int VerticalSpacing { get; set; } = 10;
      public bool CenterHorizontally { get; set; } = false;

      protected override void OnControlAdded(ControlEventArgs e) {
         base.OnControlAdded(e);
         Invalidate();

         e.Control.Resize += HandleControlResize;
      }

      protected override void OnControlRemoved(ControlEventArgs e) {
         base.OnControlRemoved(e);

         e.Control.Resize -= HandleControlResize;
      }

      private void HandleControlResize(object? sender, EventArgs e) {
         this.PerformLayout();
      }

      protected override void OnLayout(LayoutEventArgs e) {
         // Necessary to call base.OnLayout so child controls perform a layout (e.g. to compute autosize dimensions)
         // I tried manually invoking PerformLayout but that did nothing.
         base.OnLayout(e);

         var controls = this.Controls.Cast<Control>().ToArray();

         var totalWidth = ClientSize.Width;
         var widthRemaining = totalWidth;
         var controlsInCurrentRow = new List<Control>();
         var currentRowTopY = 0;
         var desiredHeight = 0;

         // precondition: we know controls[i] fits in the current row
         for (var i = 0; i < controls.Length; i++) {
            var curWidth = controls[i].Width;
            controlsInCurrentRow.Add(controls[i]);
            widthRemaining -= curWidth;

            // if we're the last control or the next control needs a new line,
            // then layout the current line and start the next line
            if (i == controls.Length - 1 || widthRemaining < controls[i + 1].Width + HorizontalSpacing) {
               var currentX = CenterHorizontally ? widthRemaining / 2 : 0;
               var greatestBottomY = 0;
               foreach (var c in controlsInCurrentRow) {
                  c.Top = currentRowTopY;
                  c.Left = currentX;

                  currentX += c.Width + HorizontalSpacing;
                  greatestBottomY = Math.Max(greatestBottomY, c.Bottom);
               }

               controlsInCurrentRow.Clear();
               widthRemaining = totalWidth;

               desiredHeight = greatestBottomY;
               currentRowTopY = greatestBottomY + VerticalSpacing;
            } else {
               widthRemaining -= HorizontalSpacing; // eat up horiz spacing before processing next control, which will be in our row
            }
         }

         if (AutoSize) {
            Height = desiredHeight;
         }
      }
   }
}
