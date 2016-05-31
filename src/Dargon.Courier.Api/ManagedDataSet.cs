using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Courier {
   [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
   public class ManagedDataSetAttribute : Attribute {
      public ManagedDataSetAttribute(string alias, string longName, Type type) {
         this.Alias = alias;
         this.LongName = longName;
         this.Type = type;
      }

      public string Alias { get; }
      public string LongName { get; }
      public Type Type { get; }
   }
}
