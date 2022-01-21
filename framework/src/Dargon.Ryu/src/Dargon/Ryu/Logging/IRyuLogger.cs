using System;
using System.Reflection;
using Dargon.Ryu.Modules;

namespace Dargon.Ryu.Logging {
   public interface IRyuLogger {
      void LoadedAssemblyFromPath(string path);
      void PreviouslyLoadedAssemblyFromPath(string path);
      void FieldModifiedByConstructorAfterInjection(FieldInfo field, object expected, object actual);
   }
}
