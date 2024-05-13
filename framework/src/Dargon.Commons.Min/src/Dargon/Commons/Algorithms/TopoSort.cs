using System;
using System.Collections.Generic;
using System.Text;
using Dargon.Commons.Collections;
using Dargon.Commons.Utilities;

namespace Dargon.Commons.Algorithms {
   public static class TopoSort {
      public static List<T> Compute<T>(List<(T, T)> Edges, List<T> Nodes) where T : class {
         var nodeToPredecessorCount = new Dictionary<T, int>(new ReferenceEqualityComparer<T>());
         foreach (var node in Nodes) {
            nodeToPredecessorCount.Add(node, 0);
         }

         var nodeToSuccessors = new ListMultiValueDictionary<T, T>();
         foreach (var (pred, succ) in Edges) {
            if (pred == null) continue;
            nodeToPredecessorCount[succ]++;
            nodeToSuccessors.Add(pred, succ);
         }

         var res = new List<T>();
         foreach (var node in Nodes) {
            if (nodeToPredecessorCount[node] == 0) {
               res.Add(node);
            }
         }

         for (var i = 0; i < res.Count; i++) {
            var n = res[i];
            if (!nodeToSuccessors.TryGetValue(n, out var successors)) continue;
            foreach (var succ in successors) {
               var c = --nodeToPredecessorCount[succ];
               if (c == 0) {
                  res.Add(succ);
               }
            }
         }

         if (res.Count != Nodes.Count) {
            throw new InvalidOperationException("Failed to find topological ordering for the given directed graph. Is there a cycle? Were all nodes and edges included?");
         }

         return res;
      }
   }
}
