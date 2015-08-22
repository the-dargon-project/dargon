using System;
using System.Collections.Generic;
using System.Net;

namespace Dargon.Courier.Identities {
   public interface ReadableCourierEndpoint {
      IPAddress InitialAddress { get; }
      IPAddress LastAddress { get; }
      Guid Identifier { get; }
      string Name { get; }
      IReadOnlyDictionary<Guid, byte[]> EnumerateProperties();
      TValue GetProperty<TValue>(Guid key);
      TValue GetPropertyOrDefault<TValue>(Guid key);
      bool TryGetProperty<TValue>(Guid key, out TValue value);
      bool Matches(Guid recipientId);
      int GetRevisionNumber();
   }
}
