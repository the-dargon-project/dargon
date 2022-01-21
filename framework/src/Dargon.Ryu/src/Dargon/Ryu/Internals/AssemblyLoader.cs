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
      private static readonly string[] kExcludedFilePathFilter = {
         // xunit spam
         "xunit", 

         // .net core reference assemblies which only contain the public interface of
         // assemblies but no implementation
         //
         // https://stackoverflow.com/questions/64925794/ref-folder-within-net-5-0-bin-folder
         "/ref/", "\\ref\\"
      };

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
         var defaultLoadContext = AssemblyLoadContext.Default;
         var alreadyLoadedAssemblyByCanonicalizedLocation = ComputeAlreadyLoadedAssemblyByCanonicalizedLocationDict(defaultLoadContext);

         var filePaths = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
         for (var i = 0; i < filePaths.Length; i++) {
            var path = filePaths[i];

            if (path.EndsWithAny(kAssemblyExtensions, StringComparison.OrdinalIgnoreCase) &&
                !path.ContainsAny(kExcludedFilePathFilter, StringComparison.OrdinalIgnoreCase)) {
               var canonicalizedPath = Path.GetFullPath(path);

               for (var attempt = 0; attempt < 2; attempt++) {
                  Console.WriteLine("Attempting to load " + path);

                  if (alreadyLoadedAssemblyByCanonicalizedLocation.TryGetValue(canonicalizedPath, out var assembly)) {
                     logger.PreviouslyLoadedAssemblyFromPath(path);
                     loadedAssemblies.Add(assembly);
                     break;
                  } else {
                     try {
                        assembly = defaultLoadContext.LoadFromAssemblyPath(path);
                        logger.LoadedAssemblyFromPath(path);
                        loadedAssemblies.Add(assembly);
                        alreadyLoadedAssemblyByCanonicalizedLocation.Add(canonicalizedPath, assembly);
                        break;
                     } catch (BadImageFormatException) {
                        // skip - probably a native dll dependency
                     } catch (FileLoadException) {
                        // if it's our first FLE, we've probably hit System.IO.FileLoadException: Assembly with same name is already loaded
                        // refresh the already-loaded dict and attempt to load again.
                        if (attempt == 0) {
                           alreadyLoadedAssemblyByCanonicalizedLocation = ComputeAlreadyLoadedAssemblyByCanonicalizedLocationDict(defaultLoadContext);
                           continue;
                        }

                        // If failure happens on the second attempt, bail
                        throw;
                     }
                  }
               }
            }
         }
      }

      private static Dictionary<string, Assembly> ComputeAlreadyLoadedAssemblyByCanonicalizedLocationDict(AssemblyLoadContext defaultLoadContext) {
         var alreadyLoadedAssemblies = defaultLoadContext.Assemblies.ToArray();
         var alreadyLoadedAssemblyByCanonicalizedLocation = alreadyLoadedAssemblies.ToDictionary(
            a => Path.GetFullPath(a.Location));
         return alreadyLoadedAssemblyByCanonicalizedLocation;
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
