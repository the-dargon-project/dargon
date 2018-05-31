using Dargon.Ryu;
using Dargon.Ryu.Extensibility;
using Dargon.Ryu.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dargon.Commons;
using NLog;

namespace Dargon.Vox.Ryu {
   public class VoxRyuExtensionModule : RyuModule, IRyuExtensionModule {
      private readonly Logger logger = LogManager.GetCurrentClassLogger();

      public override RyuModuleFlags Flags => RyuModuleFlags.Default;

      public void Loaded(IRyuExtensionArguments args) { }

      public void PreConstruction(IRyuExtensionArguments args) { }

      public void PostConstruction(IRyuExtensionArguments args) {
         var ryu = args.Container;

         // enumerate all loaded nondynamic types in current appdomain.
         // Note that ryu loads all neighboring assemblies into appdomain so
         // this is basically all dependencies our program has referenced at build.
         var allLoadedTypes = new List<Type>();
         foreach (var a in AppDomain.CurrentDomain.GetAssemblies()) {
            if (a.IsDynamic) continue;

            // https://stackoverflow.com/questions/962639/detect-if-the-type-of-an-object-is-a-type-defined-by-net-framework
            if (a.FullName.Contains("PublicKeyToken=b77a5c561934e089")) continue;
            if (a.FullName.Contains("PublicKeyToken=b03f5f7f11d50a3a")) continue;
            if (a.FullName.Contains("PublicKeyToken=31bf3856ad364e35")) continue;

            // and other libraries..
            if (a.FullName.Contains("PublicKeyToken=8d05b1bb7a6fdb6c")) continue; // xunit
            if (a.FullName.Contains("PublicKeyToken=5120e14c03d0593c")) continue; // NLog
            if (a.FullName.Contains("PublicKeyToken=407dd0808d44fbdc")) continue; // Castle
            try {
               allLoadedTypes.AddRange(a.ExportedTypes);
            } catch (FileNotFoundException e) when (e.FileName.Contains("xunit")) {
               // throws on xunit test dependencies. Not sure why, though they do
               // strange stuff we don't care about.
            }
         }

         // Filter to VoxTypes implementations to load
         var voxTypesTypesToLoad = allLoadedTypes.Where(t => t != typeof(VoxTypes) && typeof(VoxTypes).IsAssignableFrom(t))
                                               .Where(t => !t.IsAbstract).ToList();

         foreach (var voxTypeToLoad in voxTypesTypesToLoad) {
            logger.Trace($"Loading VoxTypes {voxTypeToLoad.FullName}.");

            var parameterlessCtor = voxTypeToLoad.GetConstructor(Type.EmptyTypes);
            if (parameterlessCtor == null) {
               logger.Trace("Not loading as it lacks default ctor.");
            } else {
               var instance = (VoxTypes)Activator.CreateInstance(voxTypeToLoad);
               Globals.Serializer.ImportTypes(instance);
            }
         }

         // Find type serializers registered via ryu:
         var ryuTypeSerializers = ryu.Find<ITypeSerializer>();

         if (ryuTypeSerializers.Any()) {
            throw new NotImplementedException("Vox doesn't support ITypeSerializer yet.");
         }
      }

      public void PreInitialization(IRyuExtensionArguments args) { }

      public void PostInitialization(IRyuExtensionArguments args) { }
   }
}
