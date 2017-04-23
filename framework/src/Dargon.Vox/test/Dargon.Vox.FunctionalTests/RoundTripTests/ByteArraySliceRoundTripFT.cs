using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Vox.RoundTripTests;
using Xunit;

namespace Dargon.Vox.FunctionalTests.RoundTripTests {
   public class ByteArraySliceRoundTripFT : RoundTripTest {
      public static Type[] kTestTypes = { typeof(BufferBox) };

      public ByteArraySliceRoundTripFT() : base(kTestTypes) { }

      [Fact]
      public void Run() {
         var testCases = Arrays.Create(
            10,
            i => new BufferBox {
               Buffer = Arrays.Create(i * 10, j => (byte)j),
               BufferOffset = i * 2,
               BufferLength = i * 6
            });
         MultiThreadedRoundTripTest(
            testCases, 1000, 8,
            (expected, actual) => {
               AssertTrue(
                  Bytes.ArraysEqual(
                     expected.Buffer, expected.BufferOffset, expected.BufferLength,
                     actual.Buffer, actual.BufferOffset, actual.BufferLength)
                  );
            });
      }

      public class BufferBox : ISerializableType {
         public byte[] Buffer { get; set; }
         public int BufferOffset { get; set; }
         public int BufferLength { get; set; }

         public void Serialize(IBodyWriter writer) {
            writer.Write(Buffer, BufferOffset, BufferLength);
         }

         public void Deserialize(IBodyReader reader) {
            Buffer = reader.Read<byte[]>();
            BufferOffset = 0;
            BufferLength = Buffer.Length;
         }
      }
   }
}
