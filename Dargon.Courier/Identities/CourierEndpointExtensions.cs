using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Courier.Identities {
   public static class CourierEndpointExtensions {
      public static bool TryGetProperty<TValue>(this ReadableCourierEndpoint courierEndpoint, TValue value) {
         var guid = ((GuidAttribute)typeof(TValue).GetCustomAttribute(typeof(GuidAttribute))).Value;
         return courierEndpoint.TryGetProperty(Guid.Parse(guid), out value);
      }
      public static TValue GetProperty<TValue>(this ReadableCourierEndpoint courierEndpoint) {
         var guid = ((GuidAttribute)typeof(TValue).GetCustomAttribute(typeof(GuidAttribute))).Value;
         return courierEndpoint.GetProperty<TValue>(Guid.Parse(guid));
      }
      public static TValue GetPropertyOrDefault<TValue>(this ReadableCourierEndpoint courierEndpoint) {
         var guid = ((GuidAttribute)typeof(TValue).GetCustomAttribute(typeof(GuidAttribute))).Value;
         return courierEndpoint.GetPropertyOrDefault<TValue>(Guid.Parse(guid));
      }
      public static void SetProperty<TValue>(this ManageableCourierEndpoint courierEndpoint, TValue value) {
         var guid = ((GuidAttribute)typeof(TValue).GetCustomAttribute(typeof(GuidAttribute))).Value;
         courierEndpoint.SetProperty(Guid.Parse(guid), value);
      }
   }
}
