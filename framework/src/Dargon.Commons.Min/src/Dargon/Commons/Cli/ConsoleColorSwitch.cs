using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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

      public ConsoleColorSwitch Invert() {
         ConsoleColor Inv(ConsoleColor x) => x switch {
            ConsoleColor.Black => ConsoleColor.White,
            ConsoleColor.White => ConsoleColor.Black,

            ConsoleColor.DarkBlue => ConsoleColor.DarkYellow,
            ConsoleColor.DarkYellow => ConsoleColor.DarkBlue,

            ConsoleColor.DarkGreen => ConsoleColor.DarkMagenta,
            ConsoleColor.DarkMagenta => ConsoleColor.DarkGreen,

            ConsoleColor.DarkCyan => ConsoleColor.DarkRed,
            ConsoleColor.DarkRed => ConsoleColor.DarkCyan,

            ConsoleColor.DarkGray => ConsoleColor.Gray,
            ConsoleColor.Gray => ConsoleColor.DarkGray,

            ConsoleColor.Blue => ConsoleColor.Yellow,
            ConsoleColor.Yellow => ConsoleColor.Blue,

            ConsoleColor.Green => ConsoleColor.Magenta,
            ConsoleColor.Magenta => ConsoleColor.Green,

            ConsoleColor.Cyan => ConsoleColor.Red,
            ConsoleColor.Red => ConsoleColor.Cyan,

            _ => throw new NotImplementedException($"Unable to invert console color {x}!?")
         };

         return To(Inv(Console.ForegroundColor), Inv(Console.BackgroundColor));
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
