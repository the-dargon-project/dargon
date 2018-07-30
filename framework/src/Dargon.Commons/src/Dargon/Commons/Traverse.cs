using System;
using System.Collections.Generic;
using System.Linq;

namespace Dargon.Commons {
   public static class Traverse {
      public static IEnumerable<T> Dfs<T>(this T root, Func<T, IEnumerable<T>> next) {
         var s = new Stack<T>();
         s.Push(root);

         var temp = new Stack<T>();

         while (s.Count != 0) {
            var n = s.Pop();
            yield return n;

            foreach (var c in next(n)) temp.Push(c);
            while (temp.Count != 0) s.Push(temp.Pop());
         }
      }

      public static IEnumerable<T> Bfs<T>(this T root, Func<T, IEnumerable<T>> next) {
         var q = new Queue<T>();
         q.Enqueue(root);

         while (q.Count != 0) {
            var n = q.Dequeue();
            yield return n;

            foreach (var c in next(n)) q.Enqueue(c);
         }
      }

      public static IEnumerable<T> Dive<T>(this T root, Func<T, T> next) where T : class {
         while (root != null) {
            yield return root;
            root = next(root);
         }
      }
   }
}
