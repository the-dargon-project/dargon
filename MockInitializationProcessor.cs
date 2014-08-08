using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace libtestutil
{
   public class MockInitializationProcessor
   {
      public MockInitializationProcessor()
      {
      }

      public void Process(object mockContainer)
      {
         var type = mockContainer.GetType();
         var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
         foreach (var field in fields)
         {
            var fieldType = field.FieldType;
            if (fieldType.IsGenericType)
            {
               if (fieldType.GetGenericTypeDefinition().Name.StartsWith("Mock", StringComparison.OrdinalIgnoreCase) &&
                   fieldType.Assembly.FullName.IndexOf("moq", StringComparison.OrdinalIgnoreCase) != -1)
               {
                  var fieldValue = field.GetValue(mockContainer);
                  if (fieldValue == null)
                  {
                     InitializeMockField(mockContainer, field, fieldType);
                  }
               }
            }
         }
      }

      private void InitializeMockField(object mockContainer, FieldInfo mockField, Type fieldType)
      {
         var mockInstance = Activator.CreateInstance(fieldType);
         mockField.SetValue(mockContainer, mockInstance);
      }
   }
}
