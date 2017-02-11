using System;

namespace Dargon.Hydrous.Store {
   public class PrimaryKeyAttribute : Attribute {
      public PrimaryKeyAttribute() { }

      public PrimaryKeyAttribute(string columnName) {
         ColumnName = columnName;
      }

      public string ColumnName { get; set; }
   }
}
