using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Nest;

namespace Dargon.Repl {
   public class ReplCore {
      private readonly IDispatcher dispatcher;

      public ReplCore(IDispatcher dispatcher) {
         this.dispatcher = dispatcher;
      }

      public int Run() {
         while (true) {
            Console.Write("> ");
            var input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input)) {
               try {
                  dispatcher.Eval(input);
               } catch (Exception e) {
                  Console.Error.WriteLine(e.Message);
                  Console.Error.WriteLine(e.StackTrace);
               }
               Console.WriteLine();
            }
         }
      }
   }
}
