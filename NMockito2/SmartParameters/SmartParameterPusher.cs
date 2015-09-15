namespace NMockito2.SmartParameters {
   public class SmartParameterPusher {
      private readonly SmartParameterStore smartParameterStore;

      public SmartParameterPusher(SmartParameterStore smartParameterStore) {
         this.smartParameterStore = smartParameterStore;
      }

      public void Any<T>() {
         smartParameterStore.Push(new AnySmartParameter(typeof(T)));
      }
   }
}