namespace NMockito
{
   public class NMockitoEquals : INMockitoSmartParameter
   {
      private readonly object value;
      public NMockitoEquals(object value) { this.value = value; }
      public bool Test(object value) { return object.Equals(this.value, value); }
      public override string ToString() { return "[Equals " + value + "]"; }
      public override bool Equals(object obj) { return obj is NMockitoEquals && ((NMockitoEquals)obj).value == this.value; }
   }
}