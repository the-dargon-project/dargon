using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons {
   public static class ReflectionCache<T> {
      public static readonly Type Type = typeof(T);
      public static readonly string Name = Type.Name;
      public static readonly FieldInfo[] PublicFields = Type.GetFields(BindingFlags.Public | BindingFlags.Instance);
      public static readonly KeyValuePair<string, FieldInfo>[] PublicFieldNameAndInfos = PublicFields.Map(f => f.PairKey(f.Name));
      public static readonly Dictionary<string, FieldInfo> PublicFieldsByName = PublicFieldNameAndInfos.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
   }
}
