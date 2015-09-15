using System.Collections.Generic;
using System.Linq;
using NMockito2.Mocks;

namespace NMockito2.SmartParameters {
   public class SmartParameterCollection {
      private readonly IReadOnlyList<SmartParameter> smartParameters;

      public SmartParameterCollection(IReadOnlyList<SmartParameter> smartParameters) {
         this.smartParameters = smartParameters;
      }

      public bool Matches(InvocationDescriptor invocationDescriptor) {
         return smartParameters.Count == invocationDescriptor.Arguments.Length &&
                smartParameters.Zip(invocationDescriptor.Arguments,
                                    (smartParameter, argument) => smartParameter.Matches(argument))
                               .All(x => x);
      }
   }
}