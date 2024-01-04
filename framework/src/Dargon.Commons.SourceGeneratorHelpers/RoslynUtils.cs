using System.Collections.Generic;
using Dargon.Commons;
using Microsoft.CodeAnalysis;

namespace Dargon.Vox.SourceGenerators {
   public static class RoslynUtils {
      public static AttributeData FindAnyAttributeOrDefault(ISymbol t, List<INamedTypeSymbol> searchCandidates) {
         foreach (var attr in t.GetAttributes()) {
            var attrc = attr.AttributeClass;
            if (attrc.IsGenericType) {
               attrc = attrc.OriginalDefinition;
            }
            foreach (var candidate in searchCandidates) {
               if (SymbolEqualityComparer.Default.Equals(attrc, candidate)) {
                  return attr;
               }
            }
         }

         return null;
      }

      public static List<INamedTypeSymbol> EnumerateNamedTypeSymbols(INamespaceOrTypeSymbol searchStart) {
         var res = new List<INamedTypeSymbol>();
         void Inner(INamespaceOrTypeSymbol cur) {
            if (cur is INamedTypeSymbol nts) {
               res.Add(nts);

               foreach (var x in nts.GetTypeMembers()) {
                  Inner(x);
               }
            }

            if (cur is INamespaceSymbol ns) {
               foreach (var x in ns.GetNamespaceMembers()) {
                  Inner(x);
               }
               foreach (var x in ns.GetTypeMembers()) {
                  Inner(x);
               }
            }
         }

         Inner(searchStart);
         return res;
      }

      public static List<INamedTypeSymbol> FilterTypeDescendentsAndSelf(List<INamedTypeSymbol> haystack, INamedTypeSymbol baseType) {
         baseType.TypeKind.AssertEquals(TypeKind.Class);

         var res = new List<INamedTypeSymbol> { baseType };
         foreach (var t in haystack) {
            for (var current = t; current != null; current = current.BaseType) {
               if (SymbolEqualityComparer.Default.Equals(current.BaseType, baseType)) {
                  res.Add(t);
               }
            }
         }

         return res;
      }
   }
}