using Dargon.Commons.Templating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.Utilities;
using FieldInfo = System.Reflection.FieldInfo;

namespace Dargon.Commons {
   public class ReflectionCache {
      private static ReaderWriterLockSlim sync = new(LockRecursionPolicy.SupportsRecursion);
      private static Dictionary<Type, ReflectionCache> cacheByType = new();

      public static ReflectionCache OfType(Type t) {
         using var guard = sync.CreateUpgradableReaderGuard(GuardState.SimpleReader);
         if (cacheByType.TryGetValueWithDoubleCheckedLock(t, out var cache, guard.Scoped)) {
            return cache;
         }

         guard.UpgradeToWriterLock();
         return cacheByType[t] = new(t);
      }

      private ReflectionCache(Type type) {
         Type = type;
         Name = Type.Name;
         BaseType = type.BaseType;

         var declaredMembers = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
         Members = BaseType == null ? declaredMembers : Arrays.Concat(OfType(BaseType).Members, declaredMembers);
         Fields = Members.MapFilterToNotNull(m => m as FieldInfo);
         InstanceFields = Fields.FilterTo(f => !f.IsStatic);
         PublicInstanceFields = InstanceFields.FilterTo(f => f.IsPublic);
         PublicInstanceFieldNameAndInfos = PublicInstanceFields.Map(f => f.PairKey(f.Name));
         PublicInstanceFieldsByName = PublicInstanceFieldNameAndInfos.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

         IsUnmanaged = type.IsPrimitive || type.IsPointer || type.IsEnum ||
                       (type.IsValueType && InstanceFields.All(f => OfType(f.FieldType).IsUnmanaged));
      }

      public readonly Type Type;
      public readonly string Name;
      public readonly Type BaseType;
      public readonly MemberInfo[] Members;

      public readonly FieldInfo[] Fields;
      public readonly FieldInfo[] InstanceFields;
      public readonly FieldInfo[] PublicInstanceFields;
      public readonly KeyValuePair<string, FieldInfo>[] PublicInstanceFieldNameAndInfos;
      public readonly Dictionary<string, FieldInfo> PublicInstanceFieldsByName;
      
      public readonly bool IsUnmanaged;
   }

   public static class ReflectionCache<T> {
      public static readonly ReflectionCache Cache = ReflectionCache.OfType(typeof(T));
      public static Type Type => Cache.Type;
      public static string Name => Cache.Name;
      public static MemberInfo[] Members => Cache.Members;

      public static FieldInfo[] Fields => Cache.Fields;
      public static FieldInfo[] InstanceFields => Cache.InstanceFields;
      public static FieldInfo[] PublicInstanceFields => Cache.PublicInstanceFields;
      public static KeyValuePair<string, FieldInfo>[] PublicInstanceFieldNameAndInfos => Cache.PublicInstanceFieldNameAndInfos;
      public static Dictionary<string, FieldInfo> PublicInstanceFieldsByName => Cache.PublicInstanceFieldsByName;
      
      public static bool IsUnmanaged => Cache.IsUnmanaged;

      public static class MethodLookup<TName, TBindingFlags>
         where TName : struct, ITemplateString
         where TBindingFlags : struct, ITemplateBindingFlags {
         public static MethodInfo MethodInfo = Type.GetMethod(default(TName).Value, default(TBindingFlags).Value);
      }
   }
}
