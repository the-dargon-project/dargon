﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Courier.Identities;
using Dargon.Courier.PortableObjects;
using Dargon.PortableObjects;
using ItzWarty.Threading;

namespace Dargon.Courier.Messaging {
   public interface PeriodicAnnouncer {
      void Start();
   }

   public class PeriodicAnnouncerImpl : PeriodicAnnouncer {
      private readonly ICancellationTokenSource cancellationTokenSource;
      private readonly IPofSerializer courierSerializer;
      private readonly ReadableCourierEndpoint localEndpoint;
      private readonly NetworkBroadcaster networkBroadcaster;
      private Task mainLoopTask;

      public PeriodicAnnouncerImpl(
         IThreadingProxy threadingProxy, 
         IPofSerializer courierSerializer,
         ReadableCourierEndpoint localEndpoint, 
         NetworkBroadcaster networkBroadcaster
      ) : this(
         threadingProxy.CreateCancellationTokenSource(),
         courierSerializer,
         localEndpoint, 
         networkBroadcaster) {
      }

      public PeriodicAnnouncerImpl(ICancellationTokenSource cancellationTokenSource, IPofSerializer courierSerializer, ReadableCourierEndpoint localEndpoint, NetworkBroadcaster networkBroadcaster) {
         this.cancellationTokenSource = cancellationTokenSource;
         this.courierSerializer = courierSerializer;
         this.localEndpoint = localEndpoint;
         this.networkBroadcaster = networkBroadcaster;
      }

      public void Start() {
         this.mainLoopTask = MainLoopAsync();
      }

      private async Task MainLoopAsync() {
         var cancellationToken = cancellationTokenSource.Token;
         using (var ms = new MemoryStream())
         using (var writer = new BinaryWriter(ms)) {
            while (!cancellationToken.IsCancellationRequested) {
               var versionNumber = localEndpoint.GetRevisionNumber();
               Thread.MemoryBarrier();
               courierSerializer.Serialize(writer, (object)localEndpoint.EnumerateProperties());
               networkBroadcaster.SendCourierPacket(new CourierAnnounceV1(versionNumber, ms.GetBuffer(), 0, (int)ms.Length));
               await Task.Delay(CourierAnnouncementConstants.kAnnouncementIntervalMillis, cancellationTokenSource.Token.__InnerToken);
            }
         }
      }
   }

   public static class CourierAnnouncementConstants {
      public const int kAnnouncementIntervalMillis = 500;
   }
}
