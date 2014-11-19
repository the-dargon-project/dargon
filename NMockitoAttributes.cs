using System;
using System.Diagnostics;
using System.Reflection;

namespace NMockito
{
   public static class NMockitoAttributes
   {
      public static void InitializeMocks(object target)
      {
         var type = target.GetType();
         var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
         foreach (var field in fields) {
            var mockAttribute = field.GetCustomAttribute<MockAttribute>();
            if (mockAttribute != null) {
               var fieldType = field.FieldType;
               field.SetValue(target, NMockitoStatic.CreateMock(fieldType, mockAttribute.Tracked));
            }
         }
      }
   }
}