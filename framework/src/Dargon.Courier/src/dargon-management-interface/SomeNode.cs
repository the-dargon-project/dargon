using Dargon.Commons;
using Dargon.Courier.ManagementTier;
using System.Collections.Generic;
using System.Linq;
using Dargon.Courier.ManagementTier.Vox;

namespace Dargon.Courier.Management.UI {
   public class SomeNode { 
      public string Name { get; set; }
      public SomeNode Parent { get; set; }
      public List<SomeNode> Children = new List<SomeNode>();

      public ManagementObjectIdentifierDto MobDto { get; set; }
      public MethodDescriptionDto MethodDto { get; set; }
      public PropertyDescriptionDto PropertyDto { get; set; }
      public DataSetDescriptionDto DataSetDto { get; set; }

      public bool TryGetChild(string name, out SomeNode child) {
         child = Children.FirstOrDefault(c => c.Name == name);
         return child != null;
      }

      public SomeNode GetOrAddChild(string name) {
         SomeNode child;
         if (!TryGetChild(name, out child)) {
            child = new SomeNode { Parent = this,  Name = name };
            Children.Add(child);
         }
         return child;
      }

      public override string ToString() {
         if (MethodDto != null) {
            var paramsString = MethodDto.Parameters.Map(p => $"{p.Name}: {p.Type.Name}").Join(", ");
            return $"{Name} ({paramsString}): {MethodDto.ReturnType.Name}";
         } else {
            return Name;
         }
      }
   }
}
