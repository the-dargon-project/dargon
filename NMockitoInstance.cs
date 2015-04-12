using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Assert = Xunit.Assert;

namespace NMockito
{
   public class NMockitoInstance
   {
      public NMockitoInstance()
      {
         NMockitoStatic.ReinitializeMocks(this);
      }

      [DebuggerHidden] public T CreateMock<T>() where T : class { return NMockitoStatic.CreateMock<T>(); }
      [DebuggerHidden] public object CreateMock(Type type) { return NMockitoStatic.CreateMock(type); }
      [DebuggerHidden] public T CreateUntrackedMock<T>() where T : class { return NMockitoStatic.CreateUntrackedMock<T>(); }
      [DebuggerHidden] public object CreateUntrackedMock(Type type) { return NMockitoStatic.CreateUntrackedMock(type); }
      [DebuggerHidden] public T CreateRef<T>() where T : class { return NMockitoStatic.CreateRef<T>(); }

      [DebuggerHidden] public INMockitoTimesMatcher AnyTimes() { return NMockitoStatic.AnyTimes(); }
      [DebuggerHidden] public INMockitoTimesMatcher AnyOrNoneTimes() { return NMockitoStatic.AnyOrNoneTimes(); }
      [DebuggerHidden] public INMockitoTimesMatcher Times(int count) { return NMockitoStatic.Times(count); }
      [DebuggerHidden] public INMockitoTimesMatcher Never() {  return NMockitoStatic.Never(); }
      [DebuggerHidden] public INMockitoTimesMatcher Once() { return NMockitoStatic.Once(); }

      [DebuggerHidden] public WhenContext<object> When(Expression<Action> expression) { return NMockitoStatic.When(expression); }
      [DebuggerHidden] public WhenContext<T> When<T>(T value) { return NMockitoStatic.When(value); }

      [DebuggerHidden] public T Verify<T>(T mock, INMockitoTimesMatcher times = null, NMockitoOrder order = NMockitoOrder.DontCare) where T : class { return NMockitoStatic.Verify(mock, times, order); }

      [DebuggerHidden] public void VerifyNoMoreInteractions() { NMockitoStatic.VerifyNoMoreInteractions(); }
      [DebuggerHidden] public void VerifyNoMoreInteractions<T>(T mock) { NMockitoStatic.VerifyNoMoreInteractions(mock); }

      [DebuggerHidden] public void ClearInteractions() { NMockitoStatic.ClearInteractions(); }
      [DebuggerHidden] public void ClearInteractions<T>(T mock) { NMockitoStatic.ClearInteractions(mock); }
      [DebuggerHidden] public void ClearInteractions<T>(T mock, int expectedCount) { NMockitoStatic.ClearInteractions(mock, expectedCount); }

      #region Assertions
      [DebuggerHidden] public void AssertEquals<T>(T expected, T actual) { NMockitoStatic.AssertEquals(expected, actual); }
      [DebuggerHidden] public void AssertNotEquals<T>(T expected, T actual) { NMockitoStatic.AssertNotEquals(expected, actual); }
      [DebuggerHidden] public void AssertNull<T>(T value) { NMockitoStatic.AssertNull(value); }
      [DebuggerHidden] public void AssertNotNull<T>(T value) { NMockitoStatic.AssertNotNull(value); }
      [DebuggerHidden] public void AssertTrue(bool value) { NMockitoStatic.AssertTrue(value); }
      [DebuggerHidden] public void AssertFalse(bool value) { NMockitoStatic.AssertFalse(value); }
      [DebuggerHidden] public void AssertThrows<TException>(Action action) where TException : Exception { NMockitoStatic.AssertThrows<TException>(action); }
      #endregion

      #region Smart Parameters
      [DebuggerHidden] public T Eq<T>(T value) { return NMockitoSmartParameters.Eq(value); }

      [DebuggerHidden] public TCollection EqSequence<TCollection>(TCollection value)
         where TCollection : IEnumerable {
         return NMockitoSmartParameters.EqSequence(value);
      }

      [DebuggerHidden] public T Any<T>(Func<T, bool> test = null) { return NMockitoSmartParameters.Any<T>(test); }
      #endregion

      #region Orders
      [DebuggerHidden] public NMockitoOrder DontCare() { return NMockitoStatic.DontCare(); }
      [DebuggerHidden] public NMockitoOrder WithPrevious() { return NMockitoStatic.WithPrevious(); }
      [DebuggerHidden] public NMockitoOrder AfterPrevious() { return NMockitoStatic.AfterPrevious(); }
      [DebuggerHidden] public NMockitoOrder Whenever() { return NMockitoStatic.Whenever(); }
      #endregion
   }
}