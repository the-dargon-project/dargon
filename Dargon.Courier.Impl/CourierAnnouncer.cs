using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.Identities;
using Dargon.Courier.Networking;
using ItzWarty.Threading;

namespace Dargon.Courier {
   public interface CourierAnnouncer {
   }

   public class CourierAnnouncerImpl : CourierAnnouncer {
      private const int kAnnounceIntervalMilliseconds = 5000;

      private readonly IThreadingProxy threadingProxy;
      private readonly ManageableCourierEndpoint localEndpoint;
      private readonly OutboundEnvelopeManager outboundEnvelopeManager;
      private readonly IThread thread;
      private readonly ICancellationTokenSource cancellationTokenSource;

      public CourierAnnouncerImpl(
         IThreadingProxy threadingProxy,
         ManageableCourierEndpoint localEndpoint, 
         OutboundEnvelopeManager outboundEnvelopeManager) {
         this.threadingProxy = threadingProxy;
         this.localEndpoint = localEndpoint;
         this.outboundEnvelopeManager = outboundEnvelopeManager;

         this.thread = threadingProxy.CreateThread(ThreadStart, new ThreadCreationOptions() { IsBackground = true });
         this.cancellationTokenSource = threadingProxy.CreateCancellationTokenSource();
      }

      public void Initialize() {

      }

      private void ThreadStart() {
         do {
            
         } while (!cancellationTokenSource.Token.WaitForCancellation(kAnnounceIntervalMilliseconds));
      }
   }
}
