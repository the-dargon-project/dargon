using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resources {
   public static class VdmiResources {
      public static Bitmap Dargon64x64Bitmap = (Bitmap)Image.FromFile("Resources/Dargon6464.png");
      public static Icon Dargon64x64Icon = Icon.FromHandle(Dargon64x64Bitmap.GetHicon());
   }
}
