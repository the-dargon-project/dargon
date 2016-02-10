using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Ryu.Modules {
   public class ModuleConfigurationAttribute : Attribute {
      public ModuleOptionFlags Options { get; set; } = ModuleOptionFlags.None;
   }
}
