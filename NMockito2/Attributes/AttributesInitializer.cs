using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using NMockito2.Mocks;

namespace NMockito2.Attributes {
   public class AttributesInitializer {
      private readonly MockFactory mockFactory;

      public AttributesInitializer(MockFactory mockFactory) {
         this.mockFactory = mockFactory;
      }

      public void InitializeMocks(object testClassInstance) {
         var testClass = testClassInstance.GetType();
         var fieldsAndMockAttributes = from field in testClass.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                                       let mockAttribute = field.GetCustomAttribute<MockAttribute>()
                                       where mockAttribute != null
                                       select new { Field = field, MockAttribute = mockAttribute };
         foreach (var fieldsAndMockAttribute in fieldsAndMockAttributes) {
            var field = fieldsAndMockAttribute.Field;
            var mock = mockFactory.CreateMock(field.FieldType);
            field.SetValue(testClassInstance, mock);
         }
      }
   }
}
