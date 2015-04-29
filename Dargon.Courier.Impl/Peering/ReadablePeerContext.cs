using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Dargon.Courier.Identities;
using Dargon.Courier.PortableObjects;
using Dargon.PortableObjects;
using ItzWarty.Collections;

namespace Dargon.Courier.Peering {
   public interface ReadablePeerContext {
      Guid Id { get; }
   }

   public interface ManageablePeerContext : ReadablePeerContext {
      void HandlePeerAnnounce(CourierAnnounceV1 announce);
   }

   public class PeerContextImpl : ManageablePeerContext {
      private readonly ReaderWriterLockSlim synchronization = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

      private readonly IPofSerializer courierSerializer;
      private readonly Guid id;
      private readonly RevisionCounter revisionCounter;

      public PeerContextImpl(Guid id, RevisionCounter revisionCounter, IPofSerializer courierSerializer) {
         this.id = id;
         this.revisionCounter = revisionCounter;
         this.courierSerializer = courierSerializer;
      }

      public Guid Id { get { return id; } }

      public void HandlePeerAnnounce(CourierAnnounceV1 announce) {
         synchronization.EnterUpgradeableReadLock();
         try {
            if (revisionCounter.TryAdvance(announce.PropertiesRevision)) {
               synchronization.EnterWriteLock();
               try {
                  if (revisionCounter.IsCurrentCount(announce.PropertiesRevision)) {
                     using (var ms = new MemoryStream(announce.PropertiesData, announce.PropertiesDataOffset, announce.PropertiesDataLength))
                     using (var reader = new BinaryReader(ms)) {
                        var properties = (IReadOnlyDictionary<Guid, byte[]>)courierSerializer.Deserialize(reader);

                     }
                  }
               } finally {
                  synchronization.ExitWriteLock();
               }
            }
         } finally {
            synchronization.ExitUpgradeableReadLock();
         }
         // announce.PropertiesRevision
      }
   }
}