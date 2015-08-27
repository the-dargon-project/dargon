using System;
using System.Collections.Generic;
using System.Reflection;
using Castle.DynamicProxy;

namespace NMockito
{
   public interface INMockitoTimesMatcher
   {
      void MatchOrThrow(int invocations, IInvocation invocation, List<KeyValuePair<Tuple<MethodInfo, Type[], object[]>, List<int>>> invocationCountsByInvocation);
   }

   public class NMockitoTimesEqualMatcher : INMockitoTimesMatcher
   {
      private readonly int value;
      public NMockitoTimesEqualMatcher(int value) { this.value = value; }

      public void MatchOrThrow(int invocations, IInvocation invocation, List<KeyValuePair<Tuple<MethodInfo, Type[], object[]>, List<int>>> invocationCountsByInvocation) {
         if (invocations != value) {
            throw new VerificationTimesMismatchException(value.ToString(), invocations, invocation, invocationCountsByInvocation);
         }
      }

      public override string ToString() { return "[Times == " + value + "]"; }
   }

   public class NMockitoTimesAnyMatcher : INMockitoTimesMatcher
   {
      public void MatchOrThrow(int invocations, IInvocation invocation, List<KeyValuePair<Tuple<MethodInfo, Type[], object[]>, List<int>>> invocationCountsByInvocation) {
         if (invocations <= 0) {
            throw new VerificationTimesMismatchException("> 0", invocations, invocation, invocationCountsByInvocation);
         }
      }
   }

   public class NMockitoTimesAnyOrNoneMatcher : INMockitoTimesMatcher {
      public void MatchOrThrow(int invocations, IInvocation invocation, List<KeyValuePair<Tuple<MethodInfo, Type[], object[]>, List<int>>> invocationCountsByInvocation) {
         // Do nothing
      }
   }
}
