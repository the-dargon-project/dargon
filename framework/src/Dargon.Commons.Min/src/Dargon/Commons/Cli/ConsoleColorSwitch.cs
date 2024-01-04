using System;
using System.Collections.Generic;
using System.Text;

namespace Dargon.Commons.Cli {
   // From Dargon.Repl
   // https://github.com/the-dargon-project/dargon/blob/4fff99aa3b4f0f91089a722f1f18bc314cbd2680/framework/src/Dargon.Repl/src/Dargon/Repl/PrettyPrint.cs
   public class ConsoleColorSwitch : IDisposable {
      private readonly ConsoleColor initialForeground;
      private readonly ConsoleColor initialBackground;

      public ConsoleColorSwitch() {
         initialForeground = Console.ForegroundColor;
         initialBackground = Console.BackgroundColor;
      }

      public ConsoleColorSwitch To(ConsoleColor? foreground, ConsoleColor? background = null) {
         if (foreground.HasValue) Console.ForegroundColor = foreground.Value;
         if (background.HasValue) Console.BackgroundColor = background.Value;
         return this;
      }

      public void Dispose() {
         Console.ForegroundColor = initialForeground;
         Console.BackgroundColor = initialBackground;
      }
   }
}
