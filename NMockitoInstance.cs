using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NMockito
{
   public class NMockitoInstance
   {
      public void InitializeMocks() { ClearInteractions(); NMockitoAttributes.InitializeMocks(this); }

      public T CreateMock<T>() where T : class { return NMockitoStatic.CreateMock<T>(); }
      public object CreateMock(Type type) { return NMockitoStatic.CreateMock(type); }
      public T CreateUntrackedMock<T>() where T : class { return NMockitoStatic.CreateMock<T>(false); }
      public object CreateUntrackedMock(Type type) { return NMockitoStatic.CreateMock(type, false); }

      public INMockitoTimesMatcher AnyTimes() { return new NMockitoTimesAnyMatcher(); }
      public INMockitoTimesMatcher AnyOrNoneTimes() { return new NMockitoTimesAnyOrNoneMatcher(); }
      public INMockitoTimesMatcher Times(int count) { return new NMockitoTimesEqualMatcher(count); }
      public INMockitoTimesMatcher Never() {  return Times(0); }

      public WhenContext<T> When<T>(T value) { return new WhenContext<T>(); }

      public T Verify<T>(T mock, INMockitoTimesMatcher times = null, NMockitoOrder order = NMockitoOrder.DontCare) where T : class { return NMockitoStatic.Verify(mock, times, order); }

      public void VerifyNoMoreInteractions() { NMockitoStatic.VerifyNoMoreInteractions(); }
      public void VerifyNoMoreInteractions<T>(T mock) { NMockitoStatic.VerifyNoMoreInteractions(mock); }

      public void ClearInteractions() { NMockitoStatic.ClearInteractions(); }
      public void ClearInteractions<T>(T mock) { NMockitoStatic.ClearInteractions(mock); }
      public void ClearInteractions<T>(T mock, int expectedCount) { NMockitoStatic.ClearInteractions(mock, expectedCount); }

      #region Assertions
      [DebuggerHidden] public void AssertEquals<T>(T expected, T actual) { Assert.AreEqual(expected, actual); }
      [DebuggerHidden] public void AssertNull<T>(T value) { Assert.IsNull(value); }
      [DebuggerHidden] public void AssertNotNull<T>(T value) { Assert.IsNotNull(value); }
      [DebuggerHidden] public void AssertTrue(bool value) { Assert.IsTrue(value); }
      [DebuggerHidden] public void AssertFalse(bool value) { Assert.IsFalse(value); }
      #endregion

      #region Smart Parameters
      public T Eq<T>(T value) { return NMockitoSmartParameters.Eq(value); }
      public IEnumerable<T> EqSequence<T>(IEnumerable<T> value) { return NMockitoSmartParameters.EqSequence(value); }
      public T Any<T>(Func<T, bool> test = null) { return NMockitoSmartParameters.Any<T>(test); }
      #endregion

      #region Orders
      public NMockitoOrder DontCare() { return NMockitoOrder.DontCare; }
      public NMockitoOrder WithPrevious() { return NMockitoOrder.WithPrevious; }
      public NMockitoOrder AfterPrevious() { return NMockitoOrder.AfterPrevious; }
      #endregion
   }
}