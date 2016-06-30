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

      public void Serialize(IBodyWriter writer) {
         writer.Write(MultiPartMessageId);
         writer.Write(ChunkIndex);
         writer.Write(ChunkCount);
         writer.Write(Body, BodyOffset, BodyLength);
      }

      public void Deserialize(IBodyReader reader) {
         MultiPartMessageId = reader.Read<Guid>();
         ChunkIndex = reader.Read<int>();
         ChunkCount = reader.Read<int>();
         Body = reader.Read<byte[]>();
         BodyOffset = 0;
         BodyLength = Body.Length;
      }
   }
}
