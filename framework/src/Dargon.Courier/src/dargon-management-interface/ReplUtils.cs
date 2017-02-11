using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Dargon.Courier.Management.UI {
   public static class ReplUtils {
      public static void PrettyPrint(object result) {
         if (result == null) {
            Console.WriteLine("[null]");
         } else if (result is string || result.GetType().IsValueType) {
            Console.WriteLine(result);
         } else {
            var toString = result.GetType().GetMethod("ToString");
            if (toString.DeclaringType != typeof(object)) {
               Console.WriteLine(result);
            } else {
               string json = JsonConvert.SerializeObject(
                  result, Formatting.Indented,
                  new JsonConverter[] { new StringEnumConverter() });
               Console.WriteLine(json);
            }
         }
      }
   }
}
