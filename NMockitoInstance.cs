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

      public T Verify<T>(T mock, NMockitoTimes times = null) where T : class { return NMockitoStatic.Verify(mock, times); }

      public T Any<T>() { return NMockitoAnys.CreateAny<T>(); }

      public NMockitoTimes Times(int count)
      {
         var result = new NMockitoTimes(count); 
         return result;
      }

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