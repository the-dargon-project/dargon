using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Dargon.Commons.Collections;
using Dargon.Courier.AuditingTier.Utilities;

namespace Dargon.Courier.ManagementTier {
   public class MobContext {
      public ManagementObjectIdentifierDto IdentifierDto { get; set; }
      public ManagementObjectStateDto StateDto { get; set; }
      public MultiValueDictionary<string, MethodInfo> InvokableMethodsByName { get; set; }
      public Dictionary<string, PropertyInfo> InvokablePropertiesByName { get; set; }
      public object Instance { get; set; }
      public Dictionary<string, IDataPointCircularBuffer> DataSetBuffersByAlias { get; set; }
   }
}