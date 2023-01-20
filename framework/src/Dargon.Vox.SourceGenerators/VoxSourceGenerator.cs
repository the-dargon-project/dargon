﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Dargon.Vox.SourceGenerators {
   [Generator]
   public class VoxSourceGenerator : ISourceGenerator {
      private const char dq = '"';

      public void Initialize(GeneratorInitializationContext context) {
         if (!Debugger.IsAttached) {
            // Debugger.Launch();
         }  
      }

      public void Execute(GeneratorExecutionContext context) {
         var allNamedTypes = EnumerateNamedTypeSymbols(context.Compilation.GlobalNamespace).ToList();

         var voxInternalBaseDummyType = allNamedTypes.First(t => t.Name == "VoxInternalBaseDummyType");
         var voxInternalBaseAttribute = allNamedTypes.First(t => t.Name == "VoxInternalBaseAttribute");
         var allVoxTypeAttributes = FilterTypeDescendentsAndSelf(allNamedTypes, voxInternalBaseAttribute);
         var allVoxDummyTypes = FilterTypeDescendentsAndSelf(allNamedTypes, voxInternalBaseDummyType);
         var allVoxAnnotationTypes = allVoxTypeAttributes.Concat(allVoxDummyTypes).ToList();
         var voxTypeAttributeType = allNamedTypes.First(t => t.Name == "VoxTypeAttribute");

         var syntaxTreeToTypeAndVoxTypeAttribute =
            allNamedTypes.Where(t => t.TypeKind == TypeKind.Class || t.TypeKind == TypeKind.Struct)
                         .SelectPairValue(t => FindAnyAttributeOrDefault(t, allVoxTypeAttributes))
                         .Where(kvp => kvp.Value != null)
                         .GroupBy(kvp => kvp.Value.ApplicationSyntaxReference.SyntaxTree.AssertIsNotNull());

         foreach (var (syntaxTree, entries) in syntaxTreeToTypeAndVoxTypeAttribute) {
            var sb = new StringBuilder();
            sb.AppendLine($@"
                                                                                                                        // <auto-generated/>
                                                                                                                        using System;
                                                                                                                        using Dargon.Commons;
                                                                                                                        using Dargon.Vox2;
                                                                                                                        "
            );

            foreach (var (type, typeAttr) in entries) {
               var typeIsStruct = type.TypeKind == TypeKind.Struct;
               var typeKindStr = typeIsStruct ? "struct" : "class";
               var typeFullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

               if (!SymbolEqualityComparer.Default.Equals(typeAttr.AttributeClass, voxTypeAttributeType)) {
                  throw new Exception($"Expected {typeAttr.AttributeClass} to be {voxTypeAttributeType}");
               }

               int typeId = (int)typeAttr.ConstructorArguments[0].Value;
               int flags = 0;
               INamedTypeSymbol targetType = type;
               foreach (var narg in typeAttr.NamedArguments) {
                  if (narg.Key == "RedirectToType") {
                     targetType = (INamedTypeSymbol)narg.Value.Value;
                  } else if (narg.Key == "Flags") {
                     flags = (int)narg.Value.Value;
                  }
               }

               var targetIsStruct = targetType.TypeKind == TypeKind.Struct;
               var stubFull = (flags & 1 /* VoxTypeFlags.StubFull */) != 0;
               var stubRaw = (flags & 2 /* VoxTypeFlags.StubRaw */) != 0;
               var targetIsUpdatable = (flags & 4 /* VoxTypeFlags.NonUpdatable */) == 0;

               var refIfTargetIsUpdatable = targetIsUpdatable ? "ref " : "";
               var refIfTargetIsUpdatableStruct = targetIsStruct && targetIsUpdatable ? "ref " : "";

               var writeFullSb = new StringBuilder();
               var writeRawSb = new StringBuilder();
               var readFullSb = new StringBuilder();
               var readRawSb = new StringBuilder();
               var stubDefs = new StringBuilder();

               if (stubFull) {
                  writeFullSb.AppendLine($"{typeFullName}.Stub_WriteFull_{targetType.Name}(writer, {refIfTargetIsUpdatable}self);");
                  stubDefs.AppendLine($"public static partial void Stub_WriteFull_{targetType.Name}(VoxWriter writer, {refIfTargetIsUpdatableStruct}{targetType.Name} value);");

                  if (targetIsUpdatable) {
                     readFullSb.AppendLine($"{typeFullName}.Stub_ReadFullIntoRef_{targetType.Name}(reader, {refIfTargetIsUpdatable}self);");
                     stubDefs.AppendLine($"public static partial void Stub_ReadFullIntoRef_{targetType.Name}(VoxReader reader, {refIfTargetIsUpdatableStruct}{targetType.Name} value);");
                  } else {
                     readFullSb.AppendLine($"{typeFullName}.Stub_ReadFull_{targetType.Name}(reader);");
                     stubDefs.AppendLine($"public static partial {targetType.Name} Stub_ReadFull_{targetType.Name}(VoxReader reader);");
                  }
               } else {
                  writeFullSb.AppendLine($"{targetType.Name}Serializer.Instance.WriteTypeIdBytes(writer); WriteRaw(writer, ref self);");
                  if (targetIsUpdatable) {
                     readFullSb.AppendLine($"{targetType.Name}Serializer.Instance.AssertReadTypeId(reader); ReadRawIntoRef(reader, ref self);");
                  } else {
                     readFullSb.AppendLine($"{targetType.Name}Serializer.Instance.AssertReadTypeId(reader); return ReadRaw(reader);");
                  }
               }

               if (stubRaw) {
                  // public void WriteRaw(VoxWriter writer, int val) => writer.InnerWriter.Write(val);
                  // public int ReadRaw(VoxReader reader) => reader.InnerReader.ReadInt32();
                  writeRawSb.AppendLine($"{typeFullName}.Stub_WriteRaw_{targetType.Name}(writer, {refIfTargetIsUpdatable}self);");
                  stubDefs.AppendLine($"public static partial void Stub_WriteRaw_{targetType.Name}(VoxWriter writer, {refIfTargetIsUpdatableStruct}{targetType.Name} value);");
                  
                  if (targetIsUpdatable) {
                     readRawSb.AppendLine($"{typeFullName}.Stub_ReadRawIntoRef_{targetType.Name}(reader, {refIfTargetIsUpdatable}self);");
                     stubDefs.AppendLine($"public static partial void Stub_ReadRawIntoRef_{targetType.Name}(VoxReader reader, {refIfTargetIsUpdatableStruct}{targetType.Name} value);");
                  } else {
                     readRawSb.AppendLine($"return {typeFullName}.Stub_ReadRaw_{targetType.Name}(reader);");
                     stubDefs.AppendLine($"public static partial {targetType.Name} Stub_ReadRaw_{targetType.Name}(VoxReader reader);");
                  }
               } else {
                  if (targetIsUpdatable) {
                     if (!targetIsStruct) {
                        readRawSb.AppendLine($"if (self == null) throw new ArgumentNullException(nameof({targetType.Name}));");
                     }
                  } else {
                     readRawSb.AppendLine($"var self = new {targetType.Name}();");
                  }

                  // this gets auto-property fields too. In a future C# lang version they'll expose property backing
                  // fields too for getters/setters. Hopefully this'll work for that.
                  var fields = type.GetMembers().OfType<IFieldSymbol>();
                  foreach (var field in fields) {
                     var member = field.AssociatedSymbol ?? field;
                     var memberType = member is IFieldSymbol fs ? fs.Type : ((IPropertySymbol)member).Type;
                     var memberAttr = FindAnyAttributeOrDefault(member, allVoxAnnotationTypes);

                     readRawSb.AppendLine($@"
                                                                                                                                  {{");
                     var r = EmitRead(readRawSb, "", "", memberType, memberAttr?.AttributeClass);
                     readRawSb.AppendLine($@"
                                                                                                                                     self.{member.Name} = {r};
                                                                                                                                  }}");
                     // writeRawSb.Append($@"
                     //                                                                                                             writer.{(isPolymorphic ? "WritePolymorphic" : $"WriteRaw{memberType.Name}")}(self.{member.Name});");
                  }

                  if (!targetIsUpdatable) {
                     readRawSb.AppendLine($"return self;");
                  }
               }

               //                                                                                                       The below must work with strings, ints, vector3s, and hodgepodges.
               //                                                                                                       x.ReadFull/ReadRaw are optional.
               //                                                                                                       Within a serializer, write should invoke writer.WriteRaw{X}(x) if nonpolymorphic, else writer.WritePolymorphic(x)
               //                                                                                                       ... Cannot depend on interface calls on serializable, as builtins (e.g. int) don't implement the interface.
               //                                                                                                       Reader should invoke self.field = reader.ReadRaw{T}() if nonpolymorphic, else reader.ReadPolymorphic<T>()
               sb.AppendLine($@"
                                                                                                                        namespace {targetType.ContainingNamespace.ToDisplayString()} {{
                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           public static class {targetType.Name}VoxConstants {{
                                                                                                                              public const int TypeId = {typeId};
                                                                                                                              public static {targetType.Name}Serializer Serializer => {targetType.Name}Serializer.Instance;
                                                                                                                              public static readonly byte[] TypeIdBytes = TypeId.ToVariableIntBytes();
                                                                                                                           }}");


               sb.AppendLine($@"
                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           public sealed partial class {targetType.Name}Serializer : IVoxSerializer<{targetType.Name}> {{
                                                                                                                              public static readonly {targetType.Name}Serializer Instance = new();

                                                                                                                              public int[] FullTypeId {{ get; }} = new[] {{ {typeId} }};
                                                                                                                              public byte[] FullTypeIdBytes {{ get; }} = ({typeId}).ToVariableIntBytes();

                                                                                                                              public bool IsUpdatable => {(targetIsUpdatable ? "true" : "false")};

                                                                                                                              public void WriteTypeIdBytes(VoxWriter writer) => writer.WriteTypeIdBytes({targetType.Name}VoxConstants.TypeIdBytes);
                                                                                                                              public void AssertReadTypeId(VoxReader reader) => reader.AssertReadTypeIdBytes({targetType.Name}VoxConstants.TypeIdBytes);
                                                                                                                              public void WriteFull(VoxWriter writer, ref {targetType.Name} self) {{ {writeFullSb} }}
                                                                                                                              public void WriteRaw(VoxWriter writer, ref {targetType.Name} self) {{ {writeRawSb} }}
               ");

               if (targetIsUpdatable) {
                  sb.AppendLine($@"
                                                                                                                              public {targetType.Name} ReadFull(VoxReader reader) {{ {targetType.Name} res = new(); ReadFullIntoRef(reader, ref res); return res; }}
                                                                                                                              public {targetType.Name} ReadRaw(VoxReader reader) {{ {targetType.Name} res = new(); ReadRawIntoRef(reader, ref res); return res; }}
                                                                                                                              public void ReadFullIntoRef(VoxReader reader, ref {targetType.Name} self) {{ {readFullSb} }}
                                                                                                                              public void ReadRawIntoRef(VoxReader reader, ref {targetType.Name} self) {{ {readRawSb} }}
                  ");
               } else {
                  sb.AppendLine($@"
                                                                                                                              public {targetType.Name} ReadFull(VoxReader reader) {{ {readFullSb} }}
                                                                                                                              public {targetType.Name} ReadRaw(VoxReader reader) {{ {readRawSb} }}
                                                                                                                              public void ReadFullIntoRef(VoxReader reader, ref {targetType.Name} self) => throw new InvalidOperationException({dq}Reading into {targetType.Name} ref is not supported.{dq});
                                                                                                                              public void ReadRawIntoRef(VoxReader reader, ref {targetType.Name} self) => throw new InvalidOperationException({dq}Reading into {targetType.Name} ref is not supported.{dq});
                  ");
               }

               sb.AppendLine($@"
                                                                                                                           }}
               ");

               if (ReferenceEquals(type, targetType)) {
                  sb.AppendLine($@"
                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           public partial {typeKindStr} {targetType.Name} : IVoxCustomType<{targetType.Name}> {{
                                                                                                                              public int TypeId => {targetType.Name}VoxConstants.TypeId;
                                                                                                                              public {targetType.Name}Serializer Serializer => {targetType.Name}Serializer.Instance;
                                                                                                                              IVoxSerializer<{targetType.Name}> IVoxCustomType<{targetType.Name}>.Serializer => {targetType.Name}Serializer.Instance;

                                                                                                                              public void WriteFullInto(VoxWriter writer) {{ var copy = this; {targetType.Name}Serializer.Instance.WriteFull(writer, ref copy); }}
                                                                                                                              public void WriteRawInto(VoxWriter writer) {{ var copy = this; {targetType.Name}Serializer.Instance.WriteRaw(writer, ref copy); }}
                  ");

                  if (targetIsUpdatable) {
                     var copyToThisIfStruct = targetIsStruct && targetIsUpdatable ? " this = copy;" : "";
                     sb.AppendLine($@"
                                                                                                                              public void ReadFullFrom(VoxReader reader) {{ var copy = this; {targetType.Name}Serializer.Instance.ReadFullIntoRef(reader, ref copy);{copyToThisIfStruct} }}
                                                                                                                              public void ReadRawFrom(VoxReader reader) {{ var copy = this; {targetType.Name}Serializer.Instance.ReadRawIntoRef(reader, ref copy);{copyToThisIfStruct} }}
                     ");
                  }

                  sb.AppendLine($@"
                                                                                                                           }}");
               }
               sb.AppendLine($@"

                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           public static class VoxGenerated_{targetType.Name}Statics {{
                                                                                                                              public static void WriteFull{targetType.Name}(this VoxWriter writer, {targetType.Name} value) {{ var copy = value; {targetType.Name}Serializer.Instance.WriteFull(writer, ref value); }}
                                                                                                                              public static void WriteRaw{targetType.Name}(this VoxWriter writer, {targetType.Name} value) {{ var copy = value; {targetType.Name}Serializer.Instance.WriteRaw(writer, ref value); }}
                                                                                                                              public static {targetType.Name} ReadFull{targetType.Name}(this VoxReader reader) => {targetType.Name}Serializer.Instance.ReadFull(reader);
                                                                                                                              public static {targetType.Name} ReadRaw{targetType.Name}(this VoxReader reader) => {targetType.Name}Serializer.Instance.ReadRaw(reader);
                                                                                                                           }}
                                                                                                                        }}

                                                                                                                        namespace {type.ContainingNamespace.ToDisplayString()} {{
               ");
               
               if (stubDefs.Length > 0) {
                  sb.AppendLine($@"
                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           public static partial {typeKindStr} {type.Name} {{
                                                                                                                              {stubDefs}                                                               
                                                                                                                           }}");
               }

               sb.AppendLine($@"
                                                                                                                        }}");
            }

            // Add the source code to the compilation
            var hintName = $"{Path.GetFileName(syntaxTree.FilePath)}.Vox.AutoGenerated.cs";
            context.AddSource(hintName, sb.ToString());
         }
      }

      /*
       * var _arr_count = reader.ReadRawInt32();
       * var _arr = new T[_arr_count];
       * for (var _arr_i = 0; _arr_i < _arr_count; _arr_i++) {
       *   var _arr_el_dict_count = reader.ReadRawInt32();
       *   var _arr_el_dict = new Dictionary<int, int[]>();
       *   for (var _arr_el_dict_i = 0; i < _arr_el_dict_count; _arr_el_dict_i++) {
       *     var _arr_el_dict_key = reader.ReadRawInt32();
       *     var _arr_el_dict_value_arr_count = reader.ReadRawInt32();
       *     var _arr_el_dict_value_arr = new int[_arr_el_dict_value_arr_count];
       *     for (var _arr_el_dict_value_arr_i = 0; _arr_el_dict_value_arr_i < _arr_el_dict_value_arr_count; _arr_el_dict_value_arr_i++) {
       *       _arr_el_dict_value_arr[_arr_el_dict_value_arr_i] = reader.ReadRawInt32();
       *     }
       *     _arr_el_dict.add(_arr_el_dict_key, _arr_el_dict_value_arr);
       *   }
       *   _arr[_arr_i] = _arr_el_dict;
       * }
       * field = _arr;
       *
       * --
       *
       * writer.WriteRawInt32(x.Length);
       * foreach (var _arr_el in 
       */
      string EmitRead(StringBuilder sb, string ind, string baseName, ITypeSymbol t, ITypeSymbol polymorphismAttribute) {
         var isPolymorphic = polymorphismAttribute?.Name.StartsWith("P") ?? false;
         ITypeSymbol subpolyattr0 = (polymorphismAttribute as INamedTypeSymbol)?.TypeArguments.FirstOrDefault() ?? null;
         ITypeSymbol subpolyattr1 = (polymorphismAttribute as INamedTypeSymbol)?.TypeArguments.Skip(1).FirstOrDefault() ?? null;

         var classification = ClassifyType(t, out var targ0, out var targ1);
         if (classification == TypeClassification.Regular) {
            if (isPolymorphic) {
               return $"reader.ReadPolymorphic<{t.Name}>()";
            } else {
               if (t.Name == "Object") Debugger.Break();
               return $"reader.ReadRaw{t.Name}()";
            }
         } else if (classification == TypeClassification.Array) {
            // a field of type Animal can be assigned Dog (polymorphic)
            // whereas a field of type int[] cannot be assigned anything but an int[] or null.
            // to vox, this is considered nonpolymorphic (we don't need to explicitly serialize the array's type)
            isPolymorphic.AssertEquals(false);

            baseName += "_arr";
            sb.AppendLine($"{ind}var {baseName}_count = reader.ReadRawInt32();");
            sb.AppendLine($"{ind}var {baseName} = {baseName}_count == -1 ? null : {BuildArrayNewString(targ0, $"{baseName}_count")};");
            sb.AppendLine($"{ind}for (var {baseName}_i = 0; {baseName}_i < {baseName}_count; {baseName}_i++) {{");
            {
               var elres = EmitRead(sb, ind + "   ", $"{baseName}_el", targ0, subpolyattr0);
               sb.AppendLine($"{ind}   {baseName}[{baseName}_i] = {elres};");
            }
            sb.AppendLine($"{ind}}}");
            return baseName;
         } else if (classification == TypeClassification.Enumerable) {
            // We don't attempt to transmit the type of serialized collections; rather, we transmit the contained
            // data and on deserialize create a collection instance that matches the field type (which might be concrete).
            isPolymorphic.AssertEquals(false);

            baseName += "_arr";
            sb.AppendLine($"{ind}var {baseName}_count = reader.ReadRawInt32();");
            sb.AppendLine($"{ind}var {baseName} = {baseName}_count == -1 ? null : new List<{targ0.ToDisplayString()}>({baseName}_count);");
            sb.AppendLine($"{ind}for (var {baseName}_i = 0; {baseName}_i < {baseName}_count; {baseName}_i++) {{");
            {
               var elres = EmitRead(sb, ind + "   ", $"{baseName}_el", targ0, subpolyattr0);
               sb.AppendLine($"{ind}   {baseName}.Add({elres});");
            }
            sb.AppendLine($"{ind}}}");
            return baseName;
         } else if (classification == TypeClassification.DictLike) {
            baseName += "_dict";
            sb.AppendLine($"{ind}var {baseName}_count = reader.ReadRawInt32();");
            sb.AppendLine($"{ind}var {baseName} = new {t.ToDisplayString()}({baseName}_count);");
            sb.AppendLine($"{ind}for (var {baseName}_i = 0; {baseName}_i < {baseName}_count; {baseName}_i++) {{");
            {
               var keyres = EmitRead(sb, ind + "   ", $"{baseName}_key", targ0, subpolyattr0);
               sb.AppendLine($"{ind}   var {baseName}_key = {keyres};");

               var valres = EmitRead(sb, ind + "   ", $"{baseName}_val", targ1, subpolyattr1);
               sb.AppendLine($"{ind}   var {baseName}_val = {valres};");
               sb.AppendLine($"{ind}   {baseName}.Add({baseName}_key, {baseName}_val);");
            }
            sb.AppendLine($"{ind}}}");
            return baseName;
         }

         throw new NotImplementedException();
      }

      private List<INamedTypeSymbol> EnumerateNamedTypeSymbols(INamespaceOrTypeSymbol searchStart) {
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

      private List<INamedTypeSymbol> FilterTypeDescendentsAndSelf(List<INamedTypeSymbol> haystack, INamedTypeSymbol baseType) {
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

      private AttributeData FindAnyAttributeOrDefault(ISymbol t, List<INamedTypeSymbol> searchCandidates) {
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

      public enum TypeClassification {
         Regular,
         Array,
         Enumerable,
         DictLike,
      }

      private TypeClassification ClassifyType(ITypeSymbol t, out ITypeSymbol targ0, out ITypeSymbol targ1) {
         if (t is IArrayTypeSymbol ats) {
            targ0 = ats.ElementType;
            targ1 = null;
            return TypeClassification.Array;
         }

         if (t.Name == "String") {
            targ0 = targ1 = null;
            return TypeClassification.Regular;
         }

         targ0 = targ1 = null;

         foreach (var iface in t.AllInterfaces) {
            if (iface.Name.IndexOf("Dictionary", StringComparison.OrdinalIgnoreCase) >= 0 && iface.IsGenericType) {
               targ0 = iface.TypeArguments[0];
               targ1 = iface.TypeArguments[1];
               return TypeClassification.DictLike;
            }

            if (iface.Name.IndexOf("IEnumerable", StringComparison.OrdinalIgnoreCase) >= 0 && iface.IsGenericType) {
               targ0 = iface.TypeArguments[0];
            }
         }

         return targ0 != null ? TypeClassification.Enumerable : TypeClassification.Regular;
      }

      private string BuildArrayNewString(ITypeSymbol elType, string indexExpr) {
         var nestedArrayDepth = 0;
         while (elType is IArrayTypeSymbol arts) {
            nestedArrayDepth++;
            elType = arts.ElementType;
         }

         var sb = new StringBuilder();
         sb.Append("new ");
         sb.Append(elType.ToDisplayString());
         sb.Append($"[{indexExpr}]");
         for (var i = 0; i < nestedArrayDepth; i++) {
            sb.Append($"[]");
         }
         return sb.ToString();
      }
   }

   public static class Extensions {
      public static T AssertEquals<T>(this T a, T b) {
         if (!a.Equals(b)) throw new Exception($"{a} != {b}");
         return a;
      }
      public static T AssertIsNotNull<T>(this T a) where T : class {
         if (a == null) throw new Exception($"Expected non-null instance of `{typeof(T).FullName}`.");
         return a;
      }

      public static IEnumerable<KeyValuePair<T, TProj>> SelectPairValue<T, TProj>(this IEnumerable<T> e, Func<T, TProj> proj) => e.Select(x => new KeyValuePair<T, TProj>(x, proj(x)));
      public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> source, out TKey Key, out TValue Value) => (Key, Value) = (source.Key, source.Value);
      public static void Deconstruct<TKey, TValue>(this IGrouping<TKey, TValue> source, out TKey Key, out IEnumerable<TValue> Value) => (Key, Value) = (source.Key, source);
   }
}