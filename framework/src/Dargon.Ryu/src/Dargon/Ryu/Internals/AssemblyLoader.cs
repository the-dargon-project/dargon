using Dargon.Commons;
using Dargon.Ryu.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

      public Assembly x;

      public IReadOnlySet<Assembly> LoadAssembliesFromNeighboringDirectories() {
         var seedAssemblies = new[] { Assembly.GetEntryAssembly(), typeof(AssemblyLoader).GetTypeInfo().Assembly };

         // EntryAssembly can be null under unit test.
         seedAssemblies = seedAssemblies.Where(x => x != null).ToArray();

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
         var filePaths = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
         for (var i = 0; i < filePaths.Length; i++) {
            var path = filePaths[i];

            if (path.EndsWithAny(kAssemblyExtensions, StringComparison.OrdinalIgnoreCase) &&
                !path.ContainsAny(kExcludedFilePathFilter, StringComparison.OrdinalIgnoreCase)) {
               try {
                  var assembly = Assembly.LoadFrom(path);
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
            Assembly assembly;
            try {
               assembly = Assembly.Load(name);
               //assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(name);
            } catch (FileLoadException) {
               // skip - probably a native dll dependency
               continue;
            } catch (FileNotFoundException e) when (e.FileName.Contains("Microsoft")) {
               // e.g. System.IO.FileNotFoundException :
               // Could not load file or assembly 'Microsoft.Extensions.DependencyModel, Version=1.0.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'.
               // The system cannot find the file specified.
               // 
               // on netcoreapp3.1... seems to be nonfatal and due to vstest dependency?
               continue;
            }

            if (allAssemblies.Add(assembly)) {
               LoadReferencedAssemblies(assembly, allAssemblies);
            }
         }
      }
   }
}
