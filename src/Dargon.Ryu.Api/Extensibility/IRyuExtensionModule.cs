using Dargon.Ryu.Internals;
using Dargon.Ryu.Modules;

namespace Dargon.Ryu.Extensibility {
   public interface IRyuExtensionArguments {
      IRyuContainer Container { get; }
   }

   public interface IRyuExtensionModule : IRyuModule {
      void Loaded(IRyuExtensionArguments args);

      void Preconstruction(IRyuExtensionArguments args);
      void Postconstruction(IRyuExtensionArguments args);

      void Preinitialization(IRyuExtensionArguments args);
      void Postinitialization(IRyuExtensionArguments args);
   }
}