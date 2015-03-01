using System;
using System.Collections.Generic;

namespace Dargon.Courier.Identities {
   public interface ReadableCourierEndpoint {
      Guid Identifier { get; }
      string Name { get; }
      IReadOnlyDictionary<Guid, byte[]> EnumerateProperties();
      TValue GetProperty<TValue>(Guid key);
      TValue GetPropertyOrDefault<TValue>(Guid key);
      bool TryGetProperty<TValue>(Guid key, out TValue value);
   }
}
