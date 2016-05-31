using System;
using System.Collections.Generic;
using System.Linq;

namespace Dargon.Repl {
   public class ConsoleColorSwitch : IDisposable {
      private readonly ConsoleColor initialForeground;
      private readonly ConsoleColor initialBackground;

      public ConsoleColorSwitch() {
         initialForeground = Console.ForegroundColor;
         initialBackground = Console.BackgroundColor;
      }

      public ConsoleColorSwitch To(ConsoleColor foreground, ConsoleColor background) {
         Console.ForegroundColor = foreground;
         Console.BackgroundColor = background;
         return this;
      }

      public void Dispose() {
         Console.ForegroundColor = initialForeground;
         Console.BackgroundColor = initialBackground;
      }
   }

   public static class ConsoleHelpers {
      public static readonly ConsoleColor DefaultForegroundColor = Console.ForegroundColor;
      public static readonly ConsoleColor DefaultBackgroundColor = Console.BackgroundColor;
   }

   public class PrettyFormatter<T> {
      public Func<T, string> GetName { get; set; } = (T t) => t.ToString();
      public Func<T, ConsoleColor> GetForeground { get; set; } = (T t) => ConsoleHelpers.DefaultForegroundColor;
      public Func<T, ConsoleColor> GetBackground { get; set; } = (T t) => ConsoleHelpers.DefaultBackgroundColor;
   }

   public static class PrettyPrint {
      public static void List<T>(IEnumerable<T> input, PrettyFormatter<T> formatter = null) {
         if (input.Count() > 5) {
            ListMultiColumn(input, formatter);
         } else {
            ListSingleColumn(input, formatter);
         }
      }

      public static void ListMultiColumn<T>(IEnumerable<T> input, PrettyFormatter<T> formatter = null) {
         formatter = formatter ?? new PrettyFormatter<T>();
         var values = input.Select(x => x).ToArray();
         var maxValueLength = values.Max(x => formatter.GetName(x).Length);
         var maxCellLength = maxValueLength + 2;
         var consoleWidth = Console.WindowWidth - 1;
         var columnsPerRow = consoleWidth / maxCellLength;
         if (columnsPerRow <= 1) {
            ListSingleColumn(values, formatter);
            return;
         }
         var rowCount = (values.Length + columnsPerRow - 1) / columnsPerRow;
         for (var row = 0; row < rowCount; row++) {
            for (var column = 0; column < columnsPerRow; column++) {
               var i = column * rowCount + row;
               if (i < values.Length) {
                  Console.CursorLeft = column * maxCellLength;
                  var value = values[i];
                  using (new ConsoleColorSwitch().To(formatter.GetForeground(value), formatter.GetBackground(value))) {
                     Console.Write(formatter.GetName(value));
                  }
               }
            }
            Console.WriteLine();
         }
      }

      public static void ListSingleColumn<T>(IEnumerable<T> input, PrettyFormatter<T> formatter = null) {
         formatter = formatter ?? new PrettyFormatter<T>();
         foreach (var value in input.OrderBy(x => formatter.GetName(x))) {
            using (new ConsoleColorSwitch().To(formatter.GetForeground(value), formatter.GetBackground(value))) {
               Console.Write(formatter.GetName(value));
            }
            Console.WriteLine();
         }
      }
   }
}
