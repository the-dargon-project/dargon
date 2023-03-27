using Dargon.Courier.Vox;
using Dargon.Vox2;
using System;

namespace Dargon.Courier.TransportTier.Udp.Vox {
   [VoxType((int)CourierVoxTypeIds.MultiPartChunkDto, Flags = VoxTypeFlags.StubRaw)]
   public partial class MultiPartChunkDto {
      public Guid MultiPartMessageId { get; set; }
      public int ChunkIndex { get; set; }
      public int ChunkCount { get; set; }
      public byte[] Body { get; set; }
      public int BodyOffset { get; set; }
      public int BodyLength { get; set; }

      // public static partial void Stub_WriteRaw_MultiPartChunkDto(VoxWriter writer, MultiPartChunkDto x) {
      //    writer.WriteRawGuid(x.MultiPartMessageId);
      //    writer.WriteRawInt32(x.ChunkIndex);
      //    writer.WriteRawInt32(x.ChunkCount);
      //    writer.WriteRawInt32(x.BodyLength);
      //    writer.InnerWriter.Write(x.Body, x.BodyOffset, x.BodyLength);
      // }
      //
      // public static partial MultiPartChunkDto Stub_ReadRaw_MultiPartChunkDto(VoxReader reader) {
      //    var res = new MultiPartChunkDto();
      //    res.MultiPartMessageId = reader.ReadRawGuid();
      //    res.ChunkIndex = reader.ReadRawInt32();
      //    
      //    var bodyLength = reader.ReadRawInt32();
      //    res.Body = reader.InnerReader.ReadBytes(bodyLength);
      //    res.BodyOffset = 0;
      //    res.BodyLength = bodyLength;
      //    return res;
      // }
   }
}
