using Dargon.Commons;
using Dargon.Vox.Data;
using NMockito;
using System;
using System.IO;
using Dargon.Vox;
using Xunit;
using Xunit.Sdk;

namespace Dargon.Messages.Data {
   public class VariableIntegerSerializationStatics_RoundTripTests : NMockitoInstance {
      private readonly int[] edgeCaseNumbers = new[] {
         int.MinValue,
         int.MinValue + 1,
         short.MinValue - 1,
         short.MinValue,
         short.MinValue + 1,
         byte.MinValue - 1,
         byte.MinValue,
         byte.MinValue + 1,
         -(2 << 7) - 1,
         -(2 << 7),
         -(2 << 7) + 1,
         -(2 << 6) - 1,
         -(2 << 6),
         -(2 << 6) + 1,
         (2 << 6) - 1,
         (2 << 6),
         (2 << 6) + 1,
         (2 << 7) - 1,
         (2 << 7),
         (2 << 7) + 1,
         byte.MaxValue - 1,
         byte.MaxValue,
         byte.MaxValue + 1,
         short.MaxValue - 1,
         short.MaxValue,
         short.MaxValue + 1,
         int.MaxValue - 1,
         int.MaxValue
      };

      [Fact]
      public void Randomized() {
         var random = new Random(0);
         var numbers = Util.Generate(1000, i => random.Next(int.MinValue, int.MaxValue));
         RunRoundTripTest(numbers);
      }

      [Fact]
      public void EdgeCasesInts() {
         RunRoundTripTest(edgeCaseNumbers);
      }

      private void RunRoundTripTest(int[] numbers) {
         var ms = new MemoryStream();
         var writer = new ReusableStreamToForwardDataWriterAdapter();
         writer.SetTarget(ms);
         foreach (var number in numbers) {
            writer.WriteVariableInt(number);
         }
         ms.Position = 0;
         var reader = new ReusableLengthLimitedStreamToForwardDataReaderAdapter();
         reader.SetTarget(ms);
         reader.SetBytesRemaining(ms.Length);
         foreach (var number in numbers) {
            AssertEquals(number, reader.ReadVariableInt());
         }
      }

      public class IntArrayTestBox : ISerializableType {
         public int[] Value { get; set; }

         public void Serialize(ISlotWriter writer) {
            writer.WriteCollection<int, int[]>(0, Value);
         }

         public void Deserialize(ISlotReader reader) {
            Value = reader.ReadCollection<int, int[]>(0);
         }
      }
   }
}
