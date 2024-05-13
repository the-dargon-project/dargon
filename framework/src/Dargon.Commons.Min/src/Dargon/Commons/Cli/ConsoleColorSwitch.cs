using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Dargon.Commons.Cli {
   // From Dargon.Repl
   // https://github.com/the-dargon-project/dargon/blob/4fff99aa3b4f0f91089a722f1f18bc314cbd2680/framework/src/Dargon.Repl/src/Dargon/Repl/PrettyPrint.cs
   public struct ConsoleColorSwitch : IDisposable {
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

         return this.To(Inv(Console.ForegroundColor), Inv(Console.BackgroundColor));
      }

      /// <summary>
      /// Intensifies the foreground color given a fairly arbitrary mapping.
      /// </summary>
      public ConsoleColorSwitch Intensify() {
         ConsoleColor Map(ConsoleColor x) => x switch {
            ConsoleColor.Black => ConsoleColor.Red,
            ConsoleColor.White => ConsoleColor.Red,

            ConsoleColor.DarkBlue => ConsoleColor.Blue,
            ConsoleColor.DarkYellow => ConsoleColor.Yellow,
            ConsoleColor.DarkGreen => ConsoleColor.Green,
            ConsoleColor.DarkMagenta => ConsoleColor.Magenta,
            ConsoleColor.DarkCyan => ConsoleColor.Cyan,
            ConsoleColor.DarkRed => ConsoleColor.Red,
            ConsoleColor.DarkGray => ConsoleColor.Gray,

            ConsoleColor.Gray => ConsoleColor.White,
            ConsoleColor.Blue => ConsoleColor.Magenta,
            ConsoleColor.Yellow => ConsoleColor.Green,
            ConsoleColor.Green => ConsoleColor.Magenta,
            ConsoleColor.Magenta => ConsoleColor.Red,
            ConsoleColor.Cyan => ConsoleColor.Magenta,
            ConsoleColor.Red => ConsoleColor.Yellow,

            _ => throw new NotImplementedException($"Unable to intensify console color {x}!?")
         };

         return this.To(Map(Console.ForegroundColor), Console.BackgroundColor);
      }

      /// <summary>
      /// Dims the foreground color given a fairly arbitrary mapping.
      /// </summary>
      public ConsoleColorSwitch Dim() {
         ConsoleColor Map(ConsoleColor x) => x switch {
            ConsoleColor.Black => ConsoleColor.DarkGray,
            ConsoleColor.White => ConsoleColor.Gray,

            ConsoleColor.DarkBlue => ConsoleColor.Black,
            ConsoleColor.DarkYellow => ConsoleColor.DarkGray,
            ConsoleColor.DarkGreen => ConsoleColor.DarkBlue,
            ConsoleColor.DarkMagenta => ConsoleColor.Black,
            ConsoleColor.DarkCyan => ConsoleColor.Black,
            ConsoleColor.DarkRed => ConsoleColor.Black,
            ConsoleColor.DarkGray => ConsoleColor.Black,

            ConsoleColor.Gray => ConsoleColor.DarkGray,
            ConsoleColor.Blue => ConsoleColor.DarkBlue,
            ConsoleColor.Yellow => ConsoleColor.DarkYellow,
            ConsoleColor.Green => ConsoleColor.DarkGreen,
            ConsoleColor.Magenta => ConsoleColor.DarkMagenta,
            ConsoleColor.Cyan => ConsoleColor.DarkCyan,
            ConsoleColor.Red => ConsoleColor.DarkRed,

            _ => throw new NotImplementedException($"Unable to dim console color {x}!?")
         };

         return this.To(Map(Console.ForegroundColor), Console.BackgroundColor);
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

      public static ConsoleColorSwitch Set(ConsoleColor? foreground, ConsoleColor? background = null)
         => new ConsoleColorSwitch().To(foreground, background);

      public static ConsoleColorSwitch Inverted()
         => new ConsoleColorSwitch().Invert();

      public static ConsoleColorSwitch Intensified()
         => new ConsoleColorSwitch().Intensify();

      public static ConsoleColorSwitch Dimmed()
         => new ConsoleColorSwitch().Dim();
   }
}
