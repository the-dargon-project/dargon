using System;

namespace Dargon.Commons.Cli {
   public class ConsoleStyle {
      public ConsoleColor? Foreground = null;
      public ConsoleColor? Background = null;

      public ConsoleColorSwitch Apply => new ConsoleColorSwitch().To(Foreground, Background);

      public static ConsoleStyle CreateWithForeground(ConsoleColor? f) => new ConsoleStyle { Foreground = f };
      public static ConsoleStyle CreateWithBackground(ConsoleColor? b) => new ConsoleStyle { Background = b };
      public static ConsoleStyle CreateWithForegroundBackground(ConsoleColor? f, ConsoleColor? b) => new ConsoleStyle { Foreground = f, Background = b };
   }
}
