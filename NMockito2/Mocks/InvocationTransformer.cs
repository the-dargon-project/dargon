using System.Collections.Generic;
using NMockito2.Transformations;

namespace NMockito2.Mocks {
   public class InvocationTransformer {
      private readonly IReadOnlyList<InvocationTransformation> possibleTransformations;

      public InvocationTransformer(IReadOnlyList<InvocationTransformation> possibleTransformations) {
         this.possibleTransformations = possibleTransformations;
      }

      public void Forward(InvocationDescriptor invocationDescriptor) {
         foreach (var transformation in possibleTransformations) {
            if (transformation.IsApplicable(invocationDescriptor)) {
               transformation.Forward(invocationDescriptor);
               invocationDescriptor.Transformations.Add(transformation);
            }
         }
      }

      public void Backward(InvocationDescriptor invocationDescriptor) {
         var transformations = invocationDescriptor.Transformations;
         for (var i = transformations.Count - 1; i >= 0; i--) {
            var transformation = transformations[i];
            transformation.Backward(invocationDescriptor);
         }
      }
   }
}