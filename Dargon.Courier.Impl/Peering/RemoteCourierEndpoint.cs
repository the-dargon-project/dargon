using System;
using System.IO;
using System.Net;
using System.Threading;
using Dargon.Courier.Identities;
using Dargon.Courier.PortableObjects;
using Dargon.PortableObjects;
using ItzWarty;
using ItzWarty.Collections;
using SCG = System.Collections.Generic;

namespace Dargon.Courier.Peering {
   public interface RemoteCourierEndpoint : ReadableCourierEndpoint {
      void HandlePeerAnnounce(CourierAnnounceV1 announce, IPAddress remoteAddress);
   }

   public class RemoteCourierEndpointImpl : RemoteCourierEndpoint {
      private readonly object updateSynchronization = new object();
      private readonly IConcurrentDictionary<Guid, byte[]> properties = new ConcurrentDictionary<Guid, byte[]>();
      private readonly Guid identifier;
      private readonly string name;
      private readonly RevisionCounter revisionCounter;
      private readonly IPofSerializer pofSerializer;
      private readonly IPAddress initialAddress;
      private IPAddress lastAddress;

      public RemoteCourierEndpointImpl(Guid identifier, string name, RevisionCounter revisionCounter, IPofSerializer pofSerializer, IPAddress initialAddress) {
         this.identifier = identifier;
         this.name = name;
         this.revisionCounter = revisionCounter;
         this.pofSerializer = pofSerializer;
         this.initialAddress = initialAddress;
         this.lastAddress = initialAddress;
      }

      public IPAddress InitialAddress => initialAddress;
      public IPAddress LastAddress => lastAddress;
      public Guid Identifier => identifier;
      public string Name => name;

      public SCG.IReadOnlyDictionary<Guid, byte[]> EnumerateProperties() {
         return properties;
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

      public int GetRevisionNumber() {
         return revisionCounter.GetCurrentCount();
      }

      public void HandlePeerAnnounce(CourierAnnounceV1 announce, IPAddress remoteAddress) {
         if (revisionCounter.TryAdvance(announce.PropertiesRevision)) {
            lock (updateSynchronization) {
               if (revisionCounter.IsCurrentCount(announce.PropertiesRevision)) {
                  using (var ms = new MemoryStream(announce.PropertiesData, announce.PropertiesDataOffset, announce.PropertiesDataLength))
                  using (var reader = new BinaryReader(ms)) {
                     var newProperties = (SCG.IReadOnlyDictionary<Guid, byte[]>)pofSerializer.Deserialize(reader);
                     var previousKeys = new HashSet<Guid>(properties.Keys);
                     var nextKeys = new HashSet<Guid>(newProperties.Keys);
                     var keptKeys = new HashSet<Guid>(previousKeys).With(s => s.IntersectWith(nextKeys));
                     var addedKeys = new HashSet<Guid>(nextKeys).With(s => s.ExceptWith(previousKeys));
                     var removedKeys = new HashSet<Guid>(previousKeys).With(s => s.ExceptWith(nextKeys));

                     keptKeys.ForEach(k => properties[k] = newProperties[k]);
                     addedKeys.ForEach(k => properties.Add(k, newProperties[k]));
                     removedKeys.ForEach(k => properties.Remove(k));

                     this.lastAddress = remoteAddress;

                     Console.WriteLine("HPA of " + remoteAddress + " (" + identifier.ToString("N").Substring(0, 8) + ") WITH " + newProperties);
                  }
               }
            }
         }
      }
   }
}