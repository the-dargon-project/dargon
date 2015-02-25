using Dargon.PortableObjects;
using ItzWarty.Collections;
using System;
using System.IO;
using System.Linq;
using System.Text;
using ItzWarty;
using SCG = System.Collections.Generic;

namespace Dargon.Courier {
   public class CourierEndpointImpl : CourierEndpoint, IPortableObject, IEquatable<CourierEndpointImpl> {
      private readonly IPofSerializer pofSerializer;
      private Guid identifier;
      private SCG.IDictionary<Guid, byte[]> properties;

      public CourierEndpointImpl(IPofSerializer pofSerializer) {
         this.pofSerializer = pofSerializer;
      }

      public CourierEndpointImpl(IPofSerializer pofSerializer, Guid identifier) {
         this.pofSerializer = pofSerializer;
         this.identifier = identifier;
         this.properties = new ConcurrentDictionary<Guid, byte[]>();
      }

      public Guid Identifier => identifier;
      public string Name => GetPropertyOrDefault<string>(CourierEndpointPropertyKeys.Name);

      public SCG.IReadOnlyDictionary <Guid, byte[]> EnumerateProperties() {
         // properties is either an ICL.ConcurrentDictionary or an SCG.Dictionary
         return (SCG.IReadOnlyDictionary<Guid, byte[]>)properties;
      }

      public TValue GetProperty<TValue>(Guid key) {
         TValue result;
         if (!TryGetProperty(key, out result)) {
            throw new SCG.KeyNotFoundException();
         } else {
            return result;
         }
      }

      public TValue GetPropertyOrDefault<TValue>(Guid key) {
         TValue result;
         TryGetProperty(key, out result);
         return result;
      }

      public bool TryGetProperty<TValue>(Guid key, out TValue value) {
         byte[] data;
         if (!properties.TryGetValue(key, out data)) {
            value = default(TValue);
            return false;
         } else {
            value = (TValue)pofSerializer.Deserialize(new MemoryStream(data));
            return true;
         }
      }

      public void SetProperty<TValue>(Guid key, TValue value) {
         using (var ms = new MemoryStream())
         using (var writer = new BinaryWriter(ms, Encoding.UTF8, true)) {
            pofSerializer.Serialize(writer, (object)value);
            properties[key] = ms.ToArray();
         }
      }

      public void Serialize(IPofWriter writer) {
         writer.WriteGuid(0, identifier);
         writer.WriteMap(1, properties);
      }

      public void Deserialize(IPofReader reader) {
         identifier = reader.ReadGuid(0);
         properties = reader.ReadMap<Guid, byte[]>(1);
      }

      /// <summary>
      /// Performs a deep equality comparison.
      /// </summary>
      public bool Equals(CourierEndpointImpl other) {
         return other.identifier == this.identifier &&
                new SCG.HashSet<Guid>(properties.Keys).SetEquals(other.properties.Keys) &&
                properties.Keys.All(key => properties[key].SequenceEqual(other.properties[key]));
      }
   }

   public static class CourierEndpointPropertyKeys {
      public static Guid Name { get; } = Guid.Parse("{7161B692-B4E5-490F-8BB2-6AB47893B4CE}");
   }
}
