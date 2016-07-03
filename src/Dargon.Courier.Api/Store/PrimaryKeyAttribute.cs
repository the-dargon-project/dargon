using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Courier.Store {
   public class PrimaryKeyAttribute : Attribute {
      public PrimaryKeyAttribute() { }

      public PrimaryKeyAttribute(string columnName) {
         ColumnName = columnName;
      }

      public string ColumnName { get; set; }
   }
}
