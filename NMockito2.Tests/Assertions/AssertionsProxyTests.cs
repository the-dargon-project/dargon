using System;
using Xunit;
using Xunit.Sdk;

namespace NMockito2.Assertions {
   public class AssertionsProxyTests {
      private readonly AssertionsProxy testObj = new AssertionsProxy();

      [Fact]
      public void AssertEquals_HappyPathTest() => testObj.AssertEquals(1, 1);

      [Fact]
      public void AssertEquals_SadPathTest() => Assert.Throws<EqualException>(() => testObj.AssertEquals(0, 1));

      [Fact]
      public void AssertTrue_HappyPathTest() => testObj.AssertTrue(true);

      [Fact]
      public void AssertTrue_SadPathTest() => Assert.Throws<TrueException>(() => testObj.AssertTrue(false));

      [Fact]
      public void AssertFalse_HappyPathTest() => testObj.AssertFalse(false);

      [Fact]
      public void AssertFalse_SadPathTest() => Assert.Throws<FalseException>(() => testObj.AssertFalse(true));

      [Fact]
      public void AssertNull_HappyPathTest() => testObj.AssertNull(null);

      [Fact]
      public void AssertNull_SadPathTest() => Assert.Throws<NullException>(() => testObj.AssertNull(new object()));

      [Fact]
      public void AssertNotNull_HappyPathTest() => testObj.AssertNotNull(new object());

      [Fact]
      public void AssertNotNull_SadPathTest() => Assert.Throws<NotNullException>(() => testObj.AssertNotNull(null));

      [Fact]
      public void AssertThrows_WithAction_HappyPathTest() => testObj.AssertThrows<InvalidOperationException>(
         () => { throw new InvalidOperationException(); });

      [Fact]
      public void AssertThrows_WithAction_SadPathTest() => Assert.Throws<ThrowsException>(() => {
         testObj.AssertThrows<Exception>(
            () => { throw new InvalidOperationException(); });
      });

      [Fact]
      public void AssertThrows_WithInstanceAndAction_HappyPathTest() => testObj.AssertThrows<InvalidOperationException, object>(
         new object(), (o) => { throw new InvalidOperationException(); });

      [Fact]
      public void AssertThrows_WithInstanceAndAction_SadPathTest() => Assert.Throws<ThrowsException>(() => {
         testObj.AssertThrows<Exception, object>(
            new object(), (o) => { throw new InvalidOperationException(); });
      });

      [Fact]
      public void AssertWithAction_HappyPathTest() => testObj.AssertWithAction(
         () => { throw new InvalidOperationException(); }).Throws<InvalidOperationException>();

      [Fact]
      public void AssertWithAction_SadPathTest() => Assert.Throws<ThrowsException>(() => {
         testObj.AssertWithAction(() => {
            throw new InvalidOperationException(); }).Throws<Exception>();
      });
   }
}