using System;
using Xunit;

namespace NMockito2.Assertions {
   public class AssertionsProxy {
      public void AssertEquals<T>(T expected, T actual) => Assert.Equal(expected, actual);

      public void AssertTrue(bool value) => Assert.True(value);
      public void AssertFalse(bool value) => Assert.False(value);
      public void AssertNull(object obj) => Assert.Null(obj);
      public void AssertNotNull(object obj) => Assert.NotNull(obj);

      public void AssertThrows<TException>(Action action) where TException : Exception => Assert.Throws<TException>(() => action());
      public void AssertThrows<TException, TObject>(TObject obj, Action<TObject> action) where TException : Exception => AssertThrows<TException>(() => action(obj));

      public AssertWithAction AssertWithAction(Action action) => new AssertWithAction(action);
   }
}
