namespace ItzWarty.Test
{
   public class NMockitoEquals : INMockitoSmartParameter
   {
      private readonly object value;
      public NMockitoEquals(object value) { this.value = value; }
      public bool Test(object value) { return this.value.Equals(value); }
      public override string ToString() { return "[Equals " + value + "]"; }
      public override bool Equals(object obj) { return obj is NMockitoEquals && ((NMockitoEquals)obj).value == this.value; }
   }
}