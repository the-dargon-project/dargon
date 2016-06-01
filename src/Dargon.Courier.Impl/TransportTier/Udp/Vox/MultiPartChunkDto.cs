using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Vox;

namespace Dargon.Courier.TransportTier.Udp.Vox {
   public class MultiPartChunkDto : ISerializableType {
      public Guid MultiPartMessageId { get; set; }
      public int ChunkIndex { get; set; }
      public int ChunkCount { get; set; }
      public byte[] Body { get; set; }
      public int BodyOffset { get; set; }
      public int BodyLength { get; set; }

      public void Serialize(ISlotWriter writer) {
         writer.WriteGuid(0, MultiPartMessageId);
         writer.WriteNumeric(1, ChunkIndex);
         writer.WriteNumeric(2, ChunkCount);
         writer.WriteBytes(3, Body, BodyOffset, BodyLength);
      }

      public void Deserialize(ISlotReader reader) {
         MultiPartMessageId = reader.ReadGuid(0);
         ChunkIndex = reader.ReadNumeric(1);
         ChunkCount = reader.ReadNumeric(2);
         Body = reader.ReadBytes(3);
         BodyOffset = 0;
         BodyLength = Body.Length;
      }
   }
}
