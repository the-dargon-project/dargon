using System;
using Dargon.Ryu.Modules;

namespace Dargon.Ryu.Logging {
   public interface IRyuLogger {
      void LoadedAssemblyFromPath(string path);
   }
}
