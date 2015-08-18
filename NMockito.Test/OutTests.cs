using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NMockito {
   public class OutTests : NMockitoInstance {
      [Mock] private readonly IReadOnlyDictionary<string, Configuration> configurationsByName = null;
      [Mock] private readonly IReadOnlyDictionary<string, string> locationsByName = null;

      private readonly ConfigurationManager testObj;

      public OutTests() {
         testObj = new ConfigurationManager(configurationsByName);
      }

      [Fact]
      public void UnsetOutParameter_TakesDefaultValueTest() {
         const int kKey = 21337;
         var testObj = CreateMock<IReadOnlyDictionary<int, int>>();
         var outPlaceholder = CreatePlaceholder<int>();
         When(testObj.TryGetValue(kKey, out outPlaceholder)).ThenReturn(false);

         int value;
         var result = testObj.TryGetValue(kKey, out value);

         Verify(testObj).TryGetValue(kKey, out outPlaceholder);
         VerifyNoMoreInteractions();
         AssertFalse(result);
         AssertEquals(0, value);
      }

      [Fact]
      public void OutParameter_WithInterfaceTest() {
         const string kKeyName = "key_name";
         Configuration configuration = CreateMock<Configuration>();
         var configurationPlaceholder = CreatePlaceholder<Configuration>();
         When(configuration.Validate()).ThenReturn(true);
         When(configurationsByName.TryGetValue(kKeyName, out configurationPlaceholder)).Set(configurationPlaceholder, configuration).ThenReturn(true);

         AssertTrue(testObj.TryValidateConfiguration(kKeyName));

         Verify(configuration).Validate();
         Verify(configurationsByName).TryGetValue(kKeyName, out configurationPlaceholder);
         VerifyNoMoreInteractions();
      }

      [Fact]
      public void OutParameter_WithStringTest() {
         const string kKeyName = "key_name";
         const string location = "Expected Location";
         var locationPlaceholder = CreatePlaceholder<string>();
         When(locationsByName.TryGetValue(kKeyName, out locationPlaceholder)).Set(locationPlaceholder, location).ThenReturn(true);

         string actualLocation;
         var result = locationsByName.TryGetValue(kKeyName, out actualLocation);
         AssertTrue(result);
         AssertEquals(location, actualLocation);
         Verify(locationsByName).TryGetValue(kKeyName, out locationPlaceholder);
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
