using System;

namespace Dargon.Courier.Identities {
   public interface ManageableCourierEndpoint : ReadableCourierEndpoint {
      void SetProperty<TValue>(Guid key, TValue value);
   }
}