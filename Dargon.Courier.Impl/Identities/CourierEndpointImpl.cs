using System;
using System.IO;
using System.Text;
using System.Threading;
using Dargon.PortableObjects;
using ItzWarty.Collections;
using SCG = System.Collections.Generic;

namespace Dargon.Courier.Identities {
   public class CourierEndpointImpl : ManageableCourierEndpoint { 
      private readonly IPofSerializer pofSerializer;
      private readonly Guid identifier;
      private readonly IConcurrentDictionary<Guid, byte[]> properties;
      private int revisionNumber = 0;

      public CourierEndpointImpl(IPofSerializer pofSerializer, Guid identifier, string name)
         : this(pofSerializer, identifier, name, new ConcurrentDictionary<Guid, byte[]>()) {
      }

      public CourierEndpointImpl(IPofSerializer pofSerializer, Guid identifier, string name, IConcurrentDictionary<Guid, byte[]> properties) {
         this.pofSerializer = pofSerializer;
         this.identifier = identifier;
         this.properties = properties;

         SetProperty(CourierEndpointPropertyKeys.Name, name);
      }

      public Guid Identifier => identifier;
      public string Name => GetPropertyOrDefault<string>(CourierEndpointPropertyKeys.Name);

      public SCG.IReadOnlyDictionary<Guid, byte[]> EnumerateProperties() {
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

      public bool Matches(Guid recipientId) {
         return recipientId == identifier || recipientId == IdentityConstants.kBroadcastIdentityGuid;
      }

      public void SetProperty<TValue>(Guid key, TValue value) {
         using (var ms = new MemoryStream())
         using (var writer = new BinaryWriter(ms, Encoding.UTF8, true)) {
            pofSerializer.Serialize(writer, (object)value);
            properties[key] = ms.ToArray();
            Interlocked.Increment(ref revisionNumber);
         }
      }

      public int GetRevisionNumber() {
         return Interlocked.CompareExchange(ref revisionNumber, 0, 0);
      }
   }
}
