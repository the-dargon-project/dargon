namespace NMockito2.SmartParameters {
   public class EqualitySmartParameter : SmartParameter {
      private readonly object value;

      public EqualitySmartParameter(object value) {
         this.value = value;
      }

      public bool Matches(object comparedValue) => Equals(value, comparedValue);
   }
}