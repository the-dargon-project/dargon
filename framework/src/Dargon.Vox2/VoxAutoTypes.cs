using Dargon.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Vox2 {
   [AttributeUsage(AttributeTargets.Field)]
   public class TypeAttribute : Attribute {
      public TypeAttribute(Type dtoType, Type serializerType = null) {
         DtoType = dtoType;
         SerializerType = serializerType;
      }

      public Type DtoType { get; }
      public Type SerializerType { get; }
   }

   [AttributeUsage(AttributeTargets.Field)]
   public class TypeAttribute<TDtoType> : TypeAttribute {
      public TypeAttribute() : base(typeof(TDtoType), null) { }
   }

   [AttributeUsage(AttributeTargets.Field)]
   public class TypeAttribute<TDtoType, TSerializerType> : TypeAttribute {
      public TypeAttribute() : base(typeof(TDtoType), typeof(TSerializerType)) { }
   }

   public abstract class VoxAutoTypes<TTypeIds> : VoxTypes {
      private readonly List<Type> autoserializedTypes = new();
      private readonly Dictionary<Type, Type> typeToCustomSerializers = new();

      public VoxAutoTypes() {
         var tEnum = typeof(TTypeIds);
         tEnum.IsEnum.AssertIsTrue();

         var enumMembers = tEnum.GetMembers(BindingFlags.Public | BindingFlags.Static);
         foreach (var member in enumMembers) {
            foreach (var attr in member.GetCustomAttributes(true)) {
               if (attr is TypeAttribute ta) {
                  var dt = ta.DtoType;
                  if (ta.SerializerType is { } st) {
                     typeToCustomSerializers.Add(dt, st);
                  } else {
                     autoserializedTypes.Add(dt);
                  }
               }
            }
         }
      }

      public override List<Type> AutoserializedTypes => autoserializedTypes;
      public override Dictionary<Type, Type> TypeToCustomSerializers => typeToCustomSerializers;
   }
}
