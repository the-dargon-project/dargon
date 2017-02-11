using Dargon.Commons;
using NMockito;
using Xunit;

namespace Dargon.Vox.RoundTripTests {
   public class BytesIT : NMockitoInstance {
      [Fact]
      public void ByteArrays() {
         var x = Util.Generate(1024, i => (byte)(i % 256));
         RoundTripTest.Run(x);
         RoundTripTest.Run(x.SubArray(256));
         RoundTripTest.Run((byte[])null);
      }
   }
}
