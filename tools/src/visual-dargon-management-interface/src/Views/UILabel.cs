using System;
using System.Drawing;
using System.Net.Mime;
using System.Windows.Forms;
using Dargon.Commons;

namespace Dargon.Courier.Management.GUI.Views {
   /// <summary>
   /// Label w/ margin/padding/borders/etc reset by default.
   /// </summary>
   public class UILabel : Label {
      private readonly bool trim;

      public UILabel(bool trim = false) {
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

      private Size GetTextSize() => UiUtils.GetTextSize(Text, Font);
   }
}