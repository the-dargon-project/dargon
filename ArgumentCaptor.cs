namespace ItzWarty.Test
{
   public class ArgumentCaptor<T>
   {
      private readonly Capturer capturer;

      private T value;
      private bool valueIsSet;

      public ArgumentCaptor() { this.capturer = new Capturer(this); } 

      public T Capture()
      {
         NMockitoSmartParameters.__AddSmartParameter(capturer);
         return default(T);
      }

      public T Value { get { return GetValue(); } internal set { SetValue(value); } }

      private T GetValue()
      {
         if (!valueIsSet)
            throw new NMockitoValueNotYetCapturedException();
         return value;
      }

      private void SetValue(T value)
      {
         this.value = value;
         this.valueIsSet = true;
      }

      private class Capturer : INMockitoSmartParameter
      {
         private readonly ArgumentCaptor<T> argumentCaptor;
         public Capturer(ArgumentCaptor<T> argumentCaptor) { this.argumentCaptor = argumentCaptor; }

         public bool Test(object value)
         {
            argumentCaptor.Value = (T)value;
            return true;
         }
      }
   }
}