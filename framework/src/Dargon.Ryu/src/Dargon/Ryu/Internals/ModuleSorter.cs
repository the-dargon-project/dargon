using Dargon.Commons;
using Dargon.Ryu.Modules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dargon.Ryu.Internals {
   public interface IModuleSorter {
      IReadOnlyList<IRyuModule> SortModulesByInitializationOrder(IReadOnlyList<IRyuModule> modules);
   }

   public class DuplicateTypeDeclarationException : Exception {
      public DuplicateTypeDeclarationException(Type type, IRyuModule a, IRyuModule b) : base(GetMessage(type, a, b)) { }

      private static string GetMessage(Type type, IRyuModule a, IRyuModule b) {
         return $"Encountered duplicate declarations of type \"{type.FullName}\" in \"{a.Name}\" and \"{b.Name}\".";
      }
   }

   public class ModuleSorter : IModuleSorter {
      public IReadOnlyList<IRyuModule> SortModulesByInitializationOrder(IReadOnlyList<IRyuModule> modules) {
         var dependencyNodes = modules.Map(m => new ModuleNode { Module = m });
         var dependencyNodesByType = GetTypeToNodeMap(dependencyNodes);
         UpdateNodeDependencies(dependencyNodes, dependencyNodesByType);
         ValidateNoCyclicDependencies(dependencyNodes);
         AssignDependencyRanks(dependencyNodes);
         return dependencyNodes.OrderBy(n => n.InitializationRank)
                               .Select(x => x.Module).ToList();
      }

      private static IReadOnlyDictionary<Type, ModuleNode> GetTypeToNodeMap(IReadOnlyList<ModuleNode> moduleNodes) {
         var dependencyNodesByType = new Dictionary<Type, ModuleNode>();
         foreach (var dependencyNode in moduleNodes) {
            foreach (var type in dependencyNode.Types) {
               if (dependencyNodesByType.ContainsKey(type)) {
                  throw new DuplicateTypeDeclarationException(
                     type, 
                     dependencyNodesByType[type].Module, 
                     dependencyNode.Module);
               } else {
                  dependencyNodesByType[type] = dependencyNode;
               }
            }
         }
         return dependencyNodesByType;
      }

      private static void UpdateNodeDependencies(IReadOnlyList<ModuleNode> moduleNodes, IReadOnlyDictionary<Type, ModuleNode> dependencyNodesByType) {
         foreach (var moduleNode in moduleNodes) {
            foreach (var moduleType in moduleNode.Types) {
               var requiredTypes = moduleType.GetRyuConstructorParameterTypes();
               foreach (var requiredType in requiredTypes) {
                  var requiredModule = dependencyNodesByType[requiredType];
                  requiredModule.AddChild(moduleNode);
               }
            }
         }
      }

      private static void ValidateNoCyclicDependencies(IReadOnlyList<ModuleNode> moduleNodes) {
         foreach (var moduleNode in moduleNodes) {
            var visited = new HashSet<ModuleNode>();
            var stack = new Stack<ModuleNode>();
            stack.Push(moduleNode);
            while (stack.Any()) {
               var node = stack.Pop();
               if (visited.Contains(node)) {
                  throw new Exception("Cyclic dependencies found in modules.");
               }
               visited.Add(node);
               node.ChildDependencies.ForEach(stack.Push);
            }
         }
      }

      private void AssignDependencyRanks(IReadOnlyList<ModuleNode> moduleNodes) {
         foreach (var moduleNode in moduleNodes) {
            moduleNode.InitializationRank = ModuleNode.kLeafRank;
         }
         foreach (var moduleNode in moduleNodes) {
            AssignDependencyRanksHelper(moduleNode);
         }
      }

      private void AssignDependencyRanksHelper(ModuleNode node) {
         if (node.InitializationRank == ModuleNode.kLeafRank) {
            int rankSum = 1;
            foreach (var child in node.ChildDependencies) {
               AssignDependencyRanksHelper(child);
               rankSum += child.InitializationRank;
            }
            node.InitializationRank = rankSum;
         }
      }
   }
}