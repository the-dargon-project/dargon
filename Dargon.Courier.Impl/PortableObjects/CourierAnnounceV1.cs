using Dargon.PortableObjects;
using System;
using ItzWarty;

namespace Dargon.Courier.PortableObjects {
   public class CourierAnnounceV1 : IPortableObject, IEquatable<CourierAnnounceV1> {
      private int propertiesRevision;
      private byte[] propertiesData;
      private int propertiesDataOffset;
      private int propertiesDataLength;

      public CourierAnnounceV1() { }

      public CourierAnnounceV1(int propertiesRevision, byte[] propertiesData, int propertiesDataOffset, int propertiesDataLength) {
         this.propertiesRevision = propertiesRevision;
         this.propertiesData = propertiesData;
         this.propertiesDataOffset = propertiesDataOffset;
         this.propertiesDataLength = propertiesDataLength;
      }

      public int PropertiesRevision { get { return propertiesRevision; } }
      public byte[] PropertiesData { get { return propertiesData; } }
      public int PropertiesDataOffset { get { return propertiesDataOffset; } }
      public int PropertiesDataLength { get { return propertiesDataLength; } }

      public void Serialize(IPofWriter writer) {
         writer.WriteS32(0, propertiesRevision);
         writer.AssignSlot(1, propertiesData, propertiesDataOffset, propertiesDataLength);
      }

      public void Deserialize(IPofReader reader) {
         propertiesRevision = reader.ReadS32(0);
         propertiesData = reader.ReadBytes(1);
         propertiesDataOffset = 0;
         propertiesDataLength = propertiesData.Length;
      }

      public bool Equals(CourierAnnounceV1 other) {
         return propertiesRevision == other.propertiesRevision &&
                Util.ByteArraysEqual(propertiesData, other.propertiesData);
      }
   }
}
