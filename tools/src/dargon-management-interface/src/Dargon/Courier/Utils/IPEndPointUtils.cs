using System.Net;

namespace Dargon.Courier.Utils {
   /// <summary>
   /// APIs not used by Courier directly, but used in a few applications.
   /// </summary>
   public static class IPEndPointUtils {
      public static bool TryParseIpEndpoint(string s, out IPEndPoint endpoint) {
         var parts = s.Split(":");
         if (parts.Length != 2) {
            endpoint = null;
            return false;
         }
         IPAddress address;
         if (!IPAddress.TryParse(parts[0], out address)) {
            address = Dns.GetHostAddresses(parts[0])[0];
         }
         endpoint = new IPEndPoint(address, int.Parse(parts[1]));
         return true;
      }
   }
}