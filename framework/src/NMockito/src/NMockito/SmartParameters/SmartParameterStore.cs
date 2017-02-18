using System.Collections.Generic;

namespace NMockito.SmartParameters {
   public class SmartParameterStore {
      private List<SmartParameter> list = new List<SmartParameter>(); 

      public void Push(SmartParameter smartParameter) {
         list.Add(smartParameter);
      }

      public IReadOnlyList<SmartParameter> TakeAll() {
         var result = list;
         list = new List<SmartParameter>();
         return result;
      }
   }
}