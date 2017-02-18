using Dargon.Commons;
using Dargon.Ryu.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Dargon.Commons.Collections;

namespace Dargon.Ryu.Internals {
   public interface IAssemblyLoader {
      IReadOnlySet<Assembly> LoadAssembliesFromNeighboringDirectories();
   }

   public class AssemblyLoader : IAssemblyLoader {
      private static readonly string[] kAssemblyExtensions = { ".dll", ".exe" };
      private static readonly string[] kExcludedFilePathFilter = { "xunit" };
      private readonly IRyuLogger logger;

      public AssemblyLoader(IRyuLogger logger) {
         this.logger = logger;
      }

      public IReadOnlySet<Assembly> LoadAssembliesFromNeighboringDirectories() {
         var seedAssemblies = new[] { Assembly.GetEntryAssembly(), typeof(AssemblyLoader).GetTypeInfo().Assembly };

         var directoryAssemblies = new HashSet<Assembly>(seedAssemblies);
         foreach (var assembly in seedAssemblies) {
            if (assembly != null) {
               var assemblyDirectory = new FileInfo(assembly.Location).DirectoryName;
               LoadAssembliesFromDirectory(assemblyDirectory, directoryAssemblies);
            }
         }

         var allAssemblies = new HashSet<Assembly>(directoryAssemblies);
         foreach (var assembly in directoryAssemblies) {
            LoadReferencedAssemblies(assembly, allAssemblies);
         }

         return allAssemblies.AsReadOnlySet();
      }

      private void LoadAssembliesFromDirectory(string directory, HashSet<Assembly> loadedAssemblies) {
         foreach (var path in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)) {
            if (path.EndsWithAny(kAssemblyExtensions, StringComparison.OrdinalIgnoreCase) &&
                !path.ContainsAny(kExcludedFilePathFilter, StringComparison.OrdinalIgnoreCase)) {
               try {
                  var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path); 
                  logger.LoadedAssemblyFromPath(path);
                  loadedAssemblies.Add(assembly);
               } catch (BadImageFormatException) {
                  // skip - probably a native dll dependency
               }
            }
         }
      }

      private void LoadReferencedAssemblies(Assembly node, HashSet<Assembly> allAssemblies) {
         allAssemblies.Add(node);
         foreach (var name in node.GetReferencedAssemblies()) {
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(name);
            allAssemblies.Add(assembly);

            LoadReferencedAssemblies(assembly, allAssemblies);
         }
      }
   }
}
