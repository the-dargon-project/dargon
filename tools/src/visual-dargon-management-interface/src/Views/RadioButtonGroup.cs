using System.Collections.Generic;
using System.Windows.Forms;

namespace Dargon.Courier.Management.GUI.Views {
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
}