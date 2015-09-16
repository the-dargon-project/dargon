using System;
using Xunit;

namespace NMockito2.SmartParameters {
   public class AnySmartParameterTests : NMockitoInstance {
      [Fact]
      public void Matches_WithoutTypeFilter_FailsOnNull() => AssertFalse(new AnySmartParameter(null).Matches(null));

      [Fact]
      public void Matches_WithoutTypeFilter_PassesOnNonNull() => AssertTrue(new AnySmartParameter(null).Matches(123));

      [Fact]
      public void Matches_WithTypeFilter_FailsOnNull() => AssertFalse(new AnySmartParameter(typeof(AnySmartParameterTests)).Matches(null));

      [Fact]
      public void Matches_WithTypeFilter_FailsOnWrongType() => AssertFalse(new AnySmartParameter(typeof(AnySmartParameterTests)).Matches(123));

      [Fact]
      public void Matches_WithTypeFilter_PassesOnSameType() => AssertTrue(new AnySmartParameter(typeof(Exception)).Matches(new Exception()));

      [Fact]
      public void Matches_WithTypeFilter_PassesOnInheritedType() => AssertTrue(new AnySmartParameter(typeof(Exception)).Matches(new InvalidOperationException()));
   }
}