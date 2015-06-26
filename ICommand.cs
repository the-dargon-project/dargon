using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Nest {
   public interface ICommand {
      string Name { get; }
      int Eval(string args);
   }

   public interface IDispatcher {
      string FullName { get; }
      string Name { get; }
      int Eval(string args);
      IDispatcher Parent { get; set; }
   }
}
