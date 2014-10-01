
namespace ItzWarty.Test
{
   public interface INMockitoTimesMatcher
   {
      void MatchOrThrow(int invocations);
   }

   public class NMockitoTimesEqualMatcher : INMockitoTimesMatcher
   {
      private readonly int value;
      public NMockitoTimesEqualMatcher(int value) { this.value = value; }
      public void MatchOrThrow(int invocations) { if (invocations != value) throw new VerificationTimesMismatchException(value, invocations); }

      public override string ToString() { return "[Times == " + value + "]"; }
   }

   public class NMockitoTimesAnyMatcher : INMockitoTimesMatcher
   {
      public void MatchOrThrow(int invocations) { if (invocations == 0) throw new VerificationNotInvokedException(); }
   }
}
