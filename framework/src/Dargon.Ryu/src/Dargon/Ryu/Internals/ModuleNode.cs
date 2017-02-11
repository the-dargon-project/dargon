using Dargon.Ryu.Modules;
using System;
using System.Collections.Generic;

namespace Dargon.Ryu.Internals {
   public class ModuleNode {
      public const int kLeafRank = 1;

      public IRyuModule Module { get; set; }
      public IEnumerable<Type> Types => Module.TypeInfoByType.Keys;
      public HashSet<ModuleNode> ParentDependencies = new HashSet<ModuleNode>(); 
      public HashSet<ModuleNode> ChildDependencies = new HashSet<ModuleNode>();
      public int InitializationRank { get; set; } = kLeafRank;

      public void AddChild(ModuleNode moduleNode) {
         ChildDependencies.Add(moduleNode);
         moduleNode.ParentDependencies.Add(this);
      }
   }
}