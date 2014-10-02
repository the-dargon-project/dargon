using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ItzWarty.Test
{
   public class NMockitoInstance
   {
      public void InitializeMocks() { NMockitoAttributes.InitializeMocks(this); }

      public ReturnableWhenResult<T> When<T>(T value)
      {
         NMockitoWhens.HandleWhenInvocation();
         return new ReturnableWhenResult<T>();
      }

      public T Any<T>() { return NMockitoAnys.CreateAny<T>(); }

      public INMockitoTimesMatcher Times(int count) { return new NMockitoTimesEqualMatcher(count); }
      public INMockitoTimesMatcher AnyTimes() { return new NMockitoTimesAnyMatcher(); }

      public T Verify<T>(T mock, INMockitoTimesMatcher times = null) where T : class { return NMockitoStatic.Verify(mock, times); }

      public void VerifyNoMoreInteractions() { NMockitoStatic.VerifyNoMoreInteractions(); }
      public void VerifyNoMoreInteractions<T>(T mock) { NMockitoStatic.VerifyNoMoreInteractions(mock); }

      public void ClearInteractions() { NMockitoStatic.ClearInteractions(); }
      public void ClearInteractions<T>(T mock) { NMockitoStatic.ClearInteractions(mock); }

      #region Assertions
      [DebuggerHidden]
      public void AssertEquals<T>(T expected, T actual) { Assert.AreEqual(expected, actual); }

      [DebuggerHidden]
      public void AssertNull<T>(T value) { Assert.IsNull(value); }

      [DebuggerHidden]
      public void AssertNotNull<T>(T value) { Assert.IsNotNull(value); }

      [DebuggerHidden]
      public void AssertTrue(bool value) { Assert.IsTrue(value); }

      [DebuggerHidden]
      public void AssertFalse(bool value) { Assert.IsFalse(value); }
      #endregion
   }
}