using Dargon.Ryu.Internals;
using Dargon.Ryu.Modules;

namespace Dargon.Ryu.Extensibility {
   public interface IRyuExtensionArguments {
      IRyuContainer Container { get; }
   }

   public interface IRyuExtensionModule : IRyuModule {
      void Loaded(IRyuExtensionArguments args);

      void PreConstruction(IRyuExtensionArguments args);
      void PostConstruction(IRyuExtensionArguments args);

      //void PreInitialization(IRyuExtensionArguments args);
      //void PostInitialization(IRyuExtensionArguments args);
   }
}