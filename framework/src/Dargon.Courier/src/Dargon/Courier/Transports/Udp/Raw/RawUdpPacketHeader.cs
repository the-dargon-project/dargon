using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using Dargon.Commons;
using Dargon.Commons.Pooling;
using Dargon.Courier.TransportTier.Udp.Vox;

namespace Dargon.Courier.Transports.Udp.Raw {
   public enum UdpFrameType : byte {
      Unknown = 0,
      Acknowledgement = 1,
      Announcement = 2,
      Packet = 3,
   }

   /// <summary>
   /// Raw UDP packet has the structure:
   /// * Sender: Guid
   /// * Receiver: Guid
   /// * numFrames: byte
   /// * frameList: RawFrame[numFrames]:
   ///   * type: FrameType
   ///   * .ack
   ///      * MessageGuid
   ///   * .announcement
   ///      * version: short
   ///      * dataLength: short
   ///      * data: byte[]
   ///   * .packet_partial
   ///      * packetId: Guid
   ///      * flags: PacketFlags
   ///      * dataLength: short
   ///      * data: byte[]
   ///   * .packet_full
   ///      * senderId: Guid
   ///      * receiverId: Guid
   ///      * packet_partial_frame_type: byte
   ///      * packetId: Guid
   ///      * flags: PacketFlags
   ///      * dataLength: short
   ///      * data: byte[]
   /// * Signature
   /// </summary>
   public struct RawUdpPacketHeader {
      public const int kHeaderLength = 3 * 16 + 4;
      public const int kFrameListStartOffset = kHeaderLength;

      public Guid Sender;
      public Guid Receiver;
      public Guid Id;
      public PacketFlags Flags;

      public void Write(Memory<byte> buffer) {
         Sender.TryWriteBytes(buffer.Span.Slice(0, 16));
         Receiver.TryWriteBytes(buffer.Span.Slice(16, 16));
         Id.TryWriteBytes(buffer.Span.Slice(32, 16));
         BinaryPrimitives.WriteInt32LittleEndian(buffer.Span.Slice(48, 4), (int)Flags);
      }

      public static RawUdpPacketHeader Read(Span<byte> buffer) {
         return new RawUdpPacketHeader {
            Sender = new Guid(buffer.Slice(0, 16)),
            Receiver = new Guid(buffer.Slice(16, 16)),
            Id = new Guid(buffer.Slice(32, 16)),
            Flags = (PacketFlags)BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(48, 4)),
         };
      }
   }

   public ref struct RawUdpPacketFooter {
      public const int kChecksumLength = 32; // Length of a Sha256 hash

      static RawUdpPacketFooter() {
         SHA256.HashSizeInBytes.AssertEquals(kChecksumLength);
      }

      public Span<byte> Checksum;

      public void Write(Span<byte> buffer) {
         Checksum.CopyTo(buffer);
      }

      public static int GetOffset(Span<byte> buffer) => buffer.Length - kChecksumLength;

      public static RawUdpPacketFooter Read(Span<byte> buffer) {
         return new RawUdpPacketFooter {
            Checksum = buffer.Slice(GetOffset(buffer)),
         };
      }
   }

   public struct RawFrame {
      public UdpFrameType Type;
      public LeasedBufferView LeasedBuffer;
   }

   public enum RawPacketFlags {

   }
}
