using Dargon.Ryu;
using Dargon.Ryu.Extensibility;
using Dargon.Ryu.Modules;
using System;
using System.Linq;
using NLog;

namespace Dargon.Vox.Ryu {
   public class VoxRyuExtensionModule : RyuModule, IRyuExtensionModule {
      private readonly Logger logger = LogManager.GetCurrentClassLogger();

      public void Loaded(IRyuExtensionArguments args) { }

      public void PreConstruction(IRyuExtensionArguments args) { }

      public void PostConstruction(IRyuExtensionArguments args) {
         var ryu = args.Container;

         // enumerate all loaded nondynamic types in current appdomain.
         // Note that ryu loads all neighboring assemblies into appdomain so
         // this is basically all dependencies our program has referenced at build.
         var allLoadedTypes = AppDomain.CurrentDomain.GetAssemblies()
                                       .Where(a => !a.IsDynamic)
                                       .SelectMany(x => x.ExportedTypes)
                                       .ToList();

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
