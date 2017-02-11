using NMockito;
using Xunit;

namespace Dargon.Vox.RoundTripTests {
   public class BoolIT : NMockitoInstance {
      [Fact]
      public void True() => RoundTripTest.Run(true);

      [Fact]
      public void False() => RoundTripTest.Run(false);
   }
}