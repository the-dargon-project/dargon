using System;
using System.Linq;
using System.Text;
using Dargon.Commons;

namespace Dargon.Repl {
   public class ReplCore {
      private readonly IDispatcher dispatcher;

      public ReplCore(IDispatcher dispatcher) {
         this.dispatcher = dispatcher;
      }

      public int Run(string[] initialCommands = null) {
         PrintIntro();

         if (initialCommands != null) {
            Console.WriteLine("Executing initial commands.");
            foreach (var command in initialCommands) {
               Console.WriteLine("> " + command);
               ExecCommand(command);
            }
         }

         Console.WriteLine("Ready.");

         int ctrlCCount = 0;
         while (true) {
            Console.Write("> ");
            var input = ReadLineCancellable();
            if (input != null) {
               ctrlCCount = 0;
            } else {
               ctrlCCount++;
               if (ctrlCCount == 2) {
                  Console.WriteLine("Ctrl+C one more time to exit.");
               } else if (ctrlCCount == 3) {
                  return 0;
               }
            }
            if (!string.IsNullOrWhiteSpace(input)) {
               ExecCommand(input);
            }
         }
      }

      public bool ExecCommand(string input, bool trailingNewLine = true) {
         try {
            if (input.Contains("&&")) {
               return input.Split("&&").Aggregate(true, (x, cmd) => x && ExecCommand(cmd.Trim(), false));
            } else {
               return dispatcher.Eval(input) == 0;
            }
         } catch (Exception e) {
            Console.Error.WriteLine(e.Message);
            Console.Error.WriteLine(e.StackTrace);
            if (e.InnerException != null) {
               Console.Error.WriteLine(e.InnerException);
            }
            return false;
         } finally {
            if (trailingNewLine) {
               Console.WriteLine();
            }
         }
      }

      private string ReadLineCancellable() {
         Console.CancelKeyPress += (s, e) => {
            e.Cancel = true;
         };
         var cin = Console.In;
         StringBuilder sb = new StringBuilder();
         while (true) {
            int ch = cin.Read();
            if (ch == -1) {
               // control + c case
               Console.WriteLine();
               return null;
            }
            if (ch == '\r' || ch == '\n') {
               if (ch == '\r' && cin.Peek() == '\n') cin.Read();
               return sb.ToString();
            }
            sb.Append((char)ch);
         }
      }

      private void PrintIntro() {
         // Slant DMI http://patorjk.com/software/taag-v1/ 
         void PrintCenteredColored(string text, string color) {
            var lineLength = text.Split("\r\n").Last().Length;
            var padding = lineLength >= Console.BufferWidth ? "" : new string(' ', (Console.BufferWidth - lineLength) / 2);

            var textIndex = 0;
            var isFirstCharOfLine = true;
            for (var i = 0; i < color.Length;) {
               var c = color[i++];
               Console.ForegroundColor =
                  c == 'R' ? ConsoleColor.Red
                     : c == 'G' ? ConsoleColor.Green
                        : c == 'W' ? ConsoleColor.White
                           : c == 'w' ? ConsoleColor.Gray
                              : throw new ArgumentException();
               var n = color.Skip(i).TakeWhile(char.IsDigit).Join("");
               Assert.IsTrue(n.Length > 0);
               i += n.Length;

               for (var j = int.Parse(n) - 1; j >= 0 || (i == color.Length && textIndex < text.Length); j--) {
                  if (isFirstCharOfLine) {
                     Console.Write(padding);
                  }
                  Console.Write(text[textIndex]);
                  isFirstCharOfLine = text[textIndex] == '\n';
                  textIndex++;
               }
            }
            Console.WriteLine();
         }

         try {
            // https://www.text-image.com/convert/ascii.html
            // DargonNoBorderFromGitHub.png
            // Size: 35x35, Text: Black, Background: White, Invert: Yes, Contrast: Yes
            PrintCenteredColored(@"
                                                    -oo/.-/ooo:`         
                                                  /MMMMMNy+/:.`         
                                              `:sdMMMMNmh+/:.`          
                                            `oNhhNMNo-.:/osy+.          
                                            +M+`mNMMMh-`...`/dd/`       
        ____  __  ___ ____                 `yN` -dMMM:sysy-/o./Nm:      
       / __ \/  |/  //  _/               ohNMysdMMNoMMo`.s- -m:-NMo`    
      / / / / /|_/ / / /               `-mMMMMNy+-:ymm`      -N-/MMs`   
     / /_/ / /  / /_/ /               `dsmMMNy: /h+.sd/s:`    +d`hMM+   
    /_____/_/  /_//___/                :dMNso` oy`  om``om-   `m/-MMN.  
 Dargon Management Interface             `-```-m```-hM::No-    sh`NMMo  
gh://the-dargon-project/dargon          sy`:o.:h  sNd/`+N`  `` oN`MMMh  
                                       `ssdMMy`y:  `   `yd.  s`sm/MMMy  
                                       ./...`   -:-.`   `+myoN-y/NMMM+  
                                                          `/+:`+NMMMd`  
                                     +/.                   `.omMMMMd.   
                                     /+yy/`          `.:/oymMMMMMN+`    ",
               "w381W18w55W19w54W18w55W18w55W19w165R6w67R7w67R6w0");
            Console.WriteLine();
         } catch (Exception) {
            // ignored
         }
      }
   }
}
