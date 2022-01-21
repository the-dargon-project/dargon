using System;
using System.Reflection;
using Dargon.Ryu.Modules;

namespace Dargon.Ryu.Logging {
   public class RyuConsoleLoggerImpl : IRyuLogger {
      public void LoadedAssemblyFromPath(string path) {
         Console.WriteLine($"Loaded Assembly from path: {path}");
      }

      public void PreviouslyLoadedAssemblyFromPath(string path) {
         Console.WriteLine($"Previously loaded Assembly from path: {path}");
      }

      public void FieldModifiedByConstructorAfterInjection(FieldInfo field, object expected, object actual) {
         Console.WriteLine($"Warning: Dependency field modified by ctor after injection! Is there a null field initializer? " + field.DeclaringType.FullName + " " + field.Name);
      }
   }
}
