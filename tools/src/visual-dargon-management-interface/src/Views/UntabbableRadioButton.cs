using System;
using System.Windows.Forms;

namespace Dargon.Courier.Management.GUI.Views {
   public class UntabbableRadioButton : RadioButton {
      public UntabbableRadioButton() {
         TabStop = false;
      }

      protected override void OnCheckedChanged(EventArgs e) {
         base.OnCheckedChanged(e);

         TabStop = false;
      }
   }
}