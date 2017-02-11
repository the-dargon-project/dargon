using Dargon.Commons;
using Dargon.Ryu.Logging;
using System;
using System.IO;
using System.Reflection;

namespace Dargon.Ryu.Internals {
   public interface IAssemblyLoader {
      void LoadAssembliesFromNeighboringDirectories();
   }

   public class AssemblyLoader : IAssemblyLoader {
      private static readonly string[] kAssemblyExtensions = { ".dll", ".exe" };
      private static readonly string[] kExcludedFilePathFilter = { "xunit" };
      private readonly IRyuLogger logger;

      public AssemblyLoader(IRyuLogger logger) {
         this.logger = logger;
      }

      public void LoadAssembliesFromNeighboringDirectories() {
         var seedAssemblies = new[] { Assembly.GetEntryAssembly(), Assembly.GetCallingAssembly() };

         foreach (var assembly in seedAssemblies) {
            if (assembly != null) {
               var assemblyDirectory = new FileInfo(assembly.Location).DirectoryName;
               LoadAssembliesFromDirectory(assemblyDirectory);
            }
         }
      }

      public void LoadAssembliesFromDirectory(string directory) {
         foreach (var path in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)) {
            if (path.EndsWithAny(kAssemblyExtensions, StringComparison.OrdinalIgnoreCase) &&
                !path.ContainsAny(kExcludedFilePathFilter, StringComparison.OrdinalIgnoreCase)) {
               try {
                  Assembly.LoadFrom(path);
                  logger.LoadedAssemblyFromPath(path);
               } catch (BadImageFormatException) {
                  // skip - probably a native dll dependency
               }
            }
         }
      }
   }
}
