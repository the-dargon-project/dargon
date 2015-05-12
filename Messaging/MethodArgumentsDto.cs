﻿using System;
using System.Linq;
using Dargon.PortableObjects;
using ItzWarty;

namespace Dargon.Services.Messaging {
   public class MethodArgumentsDto : IPortableObject, IEquatable<MethodArgumentsDto> {
      public MethodArgumentsDto() { }

      public MethodArgumentsDto(byte[] buffer, int offset, int length) {
         Buffer = buffer;
         Offset = offset;
         Length = length;
      }

      public byte[] Buffer { get; private set; }
      public int Offset { get; private set; }
      public int Length { get; private set; }

      public void Serialize(IPofWriter writer) {
         writer.AssignSlot(0, Buffer);
         writer.WriteS32(1, Offset);
         writer.WriteS32(2, Length);
      }

      public void Deserialize(IPofReader reader) {
         Buffer = reader.ReadBytes(0);
         Offset = reader.ReadS32(1);
         Length = reader.ReadS32(2);
      }

      public bool Equals(MethodArgumentsDto other) {
         return Length == other.Length &&
                Buffer.Skip(Offset).SequenceEqual(other.Buffer.Skip(other.Offset));
      }
   }
}