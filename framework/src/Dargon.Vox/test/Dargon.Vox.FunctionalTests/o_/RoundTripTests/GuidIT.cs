using NMockito;
using System;
using Xunit;

namespace Dargon.Vox.RoundTripTests {
   public class GuidIT : NMockitoInstance {
      [Fact]
      public void Guids() {
         RoundTripTest.Run(Guid.NewGuid());
      }
   }
}
