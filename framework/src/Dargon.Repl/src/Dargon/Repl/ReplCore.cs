using System;
using System.Text;

namespace Dargon.Repl {
   public class ReplCore {
      private readonly IDispatcher dispatcher;

      public ReplCore(IDispatcher dispatcher) {
         this.dispatcher = dispatcher;
      }

      public int Run() {
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
                  return 1;
               }
            }
            if (!string.IsNullOrWhiteSpace(input)) {
               try {
                  dispatcher.Eval(input);
               } catch (Exception e) {
                  Console.Error.WriteLine(e.Message);
                  Console.Error.WriteLine(e.StackTrace);
                  if (e.InnerException != null) {
                     Console.Error.WriteLine(e.InnerException);
                  }
               }
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
   }
}
