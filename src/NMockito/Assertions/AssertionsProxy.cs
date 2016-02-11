using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Sdk;

namespace NMockito.Assertions {
   public class AssertionsProxy {
      public void AssertEquals<T>(T expected, T actual) => Assert.Equal(expected, actual);

      public void AssertSequenceEquals<T>(IEnumerable<T> expected, IEnumerable<T> actual) {
         Assert.Equal(expected, actual);
      }

      public void AssertTrue(bool value) => Assert.True(value);
      public void AssertFalse(bool value) => Assert.False(value);
      public void AssertNull(object obj) => Assert.Null(obj);
      public void AssertNotNull(object obj) => Assert.NotNull(obj);

      public void AssertThrows<TException>(Action action) where TException : Exception {
         try {
            action();
         } catch (Exception e) {
            if (e.GetType() != typeof(TException)) {
               throw new IncorrectOuterThrowException(typeof(TException), e);
            }
            return;
         }
         throw new NothingThrownException(typeof(TException));
      }

      public void AssertThrows<TOuterException, TInnerException>(Action action) where TOuterException : Exception where TInnerException : Exception {
         try {
            action();
         } catch (Exception e) {
            if (e.GetType() != typeof(TOuterException)) {
               throw new IncorrectOuterThrowException(typeof(TOuterException), e);
            } else if (!TestForInnerException<TInnerException>(e)) {
               throw new IncorrectInnerThrowException(typeof(TInnerException), e);
            }
            return;
         }
         throw new NothingThrownException(typeof(TOuterException), typeof(TInnerException));
      }

      private bool TestForInnerException<TInnerException>(Exception exception) {
         if (exception.GetType() == typeof(TInnerException)) {
            return true;
         }
         var aggregateException = exception as AggregateException;
         if (aggregateException != null) {
            foreach (var innerException in aggregateException.InnerExceptions) {
               if (TestForInnerException<TInnerException>(innerException)) {
                  return true;
               }
            }
         }
         if (exception.InnerException != null) {
            return TestForInnerException<TInnerException>(exception.InnerException);
         }
         return false;
      }

      public AssertWithAction AssertWithAction(Action action) => new AssertWithAction(this, action);
   }
}
