using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.Identities;

namespace Dargon.Courier.Peering {
   public interface ReadablePeerRegistry {
      ReadableCourierEndpoint GetRemoteCourierEndpointOrNull(Guid identifier);
      IEnumerable<ReadableCourierEndpoint> EnumeratePeers();
   }
}
