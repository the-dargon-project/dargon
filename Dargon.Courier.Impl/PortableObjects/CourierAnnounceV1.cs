using Dargon.PortableObjects;
using System;
using ItzWarty;

namespace Dargon.Courier.PortableObjects {
   public class CourierAnnounceV1 : IPortableObject, IEquatable<CourierAnnounceV1> {
      private string name;
      private int propertiesRevision;
      private byte[] propertiesData;
      private int propertiesDataOffset;
      private int propertiesDataLength;

      public CourierAnnounceV1() { }

      public CourierAnnounceV1(string name, int propertiesRevision, byte[] propertiesData, int propertiesDataOffset, int propertiesDataLength) {
         this.name = name;
         this.propertiesRevision = propertiesRevision;
         this.propertiesData = propertiesData;
         this.propertiesDataOffset = propertiesDataOffset;
         this.propertiesDataLength = propertiesDataLength;
      }

      public string Name => name;
      public int PropertiesRevision { get { return propertiesRevision; } }
      public byte[] PropertiesData { get { return propertiesData; } }
      public int PropertiesDataOffset { get { return propertiesDataOffset; } }
      public int PropertiesDataLength { get { return propertiesDataLength; } }

      public void Serialize(IPofWriter writer) {
         writer.WriteString(0, name);
         writer.WriteS32(1, propertiesRevision);
         writer.AssignSlot(2, propertiesData, propertiesDataOffset, propertiesDataLength);
      }

      public void Deserialize(IPofReader reader) {
         name = reader.ReadString(0);
         propertiesRevision = reader.ReadS32(1);
         propertiesData = reader.ReadBytes(2);
         propertiesDataOffset = 0;
         propertiesDataLength = propertiesData.Length;
      }

      public bool Equals(CourierAnnounceV1 other) {
         return name.Equals(other.Name) &&
                propertiesRevision == other.propertiesRevision &&
                Util.ByteArraysEqual(propertiesData, other.propertiesData);
      }
   }
}
