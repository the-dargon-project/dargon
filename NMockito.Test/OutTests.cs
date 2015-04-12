using System.Collections.Generic;
using Xunit;

namespace NMockito {
   public class OutTests : NMockitoInstance {
      [Mock] private readonly IReadOnlyDictionary<string, Configuration> configurationsByName = null;

      private readonly ConfigurationManager testObj;

      public OutTests() {
         testObj = new ConfigurationManager(configurationsByName);
      }

      [Fact]
      public void OutParameterTest() {
         const string kKeyName = "key_name";
         Configuration configuration = CreateMock<Configuration>();
         Configuration configurationPlaceholder = CreateMock<Configuration>();
         When(configuration.Validate()).ThenReturn(true);
         When(configurationsByName.TryGetValue(kKeyName, out configurationPlaceholder)).Set(configurationPlaceholder, configuration).ThenReturn(true);

         AssertTrue(testObj.TryValidateConfiguration(kKeyName));

         Verify(configuration).Validate();
         Verify(configurationsByName).TryGetValue(kKeyName, out configurationPlaceholder);
         VerifyNoMoreInteractions();
      }

      public class ConfigurationManager {
         private readonly IReadOnlyDictionary<string, Configuration> configurationsByName;

         public ConfigurationManager(IReadOnlyDictionary<string, Configuration> configurationsByName) {
            this.configurationsByName = configurationsByName;
         }

         public bool TryValidateConfiguration(string key) {
            Configuration configuration;
            if (configurationsByName.TryGetValue(key, out configuration)) {
               return configuration.Validate();
            } else {
               return false;
            }
         }
      }

      public interface Configuration {
         bool Validate();
      }
   }
}
