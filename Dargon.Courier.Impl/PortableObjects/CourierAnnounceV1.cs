using Dargon.PortableObjects;
using System;
using ItzWarty;

namespace Dargon.Courier.PortableObjects {
   public class CourierAnnounceV1 : IPortableObject, IEquatable<CourierAnnounceV1> {
      private int propertiesHash;
      private byte[] propertiesData;

      public CourierAnnounceV1() { }

      public CourierAnnounceV1(int propertiesHash, byte[] propertiesData) {
         this.propertiesHash = propertiesHash;
         this.propertiesData = propertiesData;
      }

      public int PropertiesHash { get { return propertiesHash; } }
      public byte[] PropertiesData { get { return propertiesData; } }

      public void Serialize(IPofWriter writer) {
         writer.WriteS32(0, propertiesHash);
         writer.WriteBytes(1, propertiesData);
      }

      public void Deserialize(IPofReader reader) {
         propertiesHash = reader.ReadS32(0);
         propertiesData = reader.ReadBytes(1);
      }

      public bool Equals(CourierAnnounceV1 other) {
         return propertiesHash == other.propertiesHash &&
                Util.ByteArraysEqual(propertiesData, other.propertiesData);
      }
   }
}
