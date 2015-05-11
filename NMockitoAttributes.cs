using System.Linq;
using System.Reflection;
using Castle.DynamicProxy.Internal;

namespace NMockito {
   public static class NMockitoAttributes
   {
      public static void InitializeMocks(object target)
      {
         var type = target.GetType();
         var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
         foreach (var field in fields) {
            var mockAttribute = field.GetCustomAttribute<MockAttribute>();
            if (mockAttribute != null) {
               InitializeMockedField(target, field, mockAttribute);
            }
         }
      }

      private static void InitializeMockedField(object target, FieldInfo mockField, MockAttribute mockAttribute) {
         var fieldType = mockField.FieldType;
         var mock = NMockitoStatic.CreateMock(fieldType, mockAttribute.Tracked);
         mockField.SetValue(target, mock);

         var staticType = mockAttribute.StaticType;
         if (staticType != null) {
            var staticFields = staticType.GetFields(BindingFlags.NonPublic | BindingFlags.Static);
            var staticInstanceFields = staticFields.Where(field => field.Name.EndsWith("instance") && field.FieldType == fieldType).ToArray();
            if (staticInstanceFields.Length != 1) {
               throw new AmbiguousMatchException("NMockito unable to determine instance field of static " + staticType.FullName + " candidates " + string.Join(", ", staticInstanceFields.Select(f=>f.Name).ToArray()));
            } else {
               staticInstanceFields.First().SetValue(null, mock);
            }
         }
      }
   }
}