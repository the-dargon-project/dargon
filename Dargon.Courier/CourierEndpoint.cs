using System;
using System.Collections.Generic;

namespace Dargon.Courier {
   public interface CourierEndpoint {
      Guid Identifier { get; }
      string Name { get; }
      IReadOnlyDictionary<Guid, byte[]> EnumerateProperties();
      TValue GetProperty<TValue>(Guid key);
      TValue GetPropertyOrDefault<TValue>(Guid key);
      bool TryGetProperty<TValue>(Guid key, out TValue value);
      void SetProperty<TValue>(Guid key, TValue value);
   }
}
