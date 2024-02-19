using System;
using Xunit.Sdk;

namespace NMockito.Fluent {
   public class FluentExceptionAssertor {
      private readonly ExceptionCaptorFactory exceptionCaptorFactory;
      private Exception lastException;

      public FluentExceptionAssertor(ExceptionCaptorFactory exceptionCaptorFactory) {
         this.exceptionCaptorFactory = exceptionCaptorFactory;
      }

      public T CreateExceptionCaptor<T>(T mock) where T : class {
         lastException = null;
         var exceptionCaptor = exceptionCaptorFactory.Create(mock, this);
         return exceptionCaptor;
      }

      public void SetLastException(Exception exception) {
         lastException = exception;
      }

      public void AssertThrown<TException>() where TException : Exception {
         if (lastException == null) {
            throw ThrowsException.ForNoException(typeof(TException));
         } else if (lastException.GetType() != typeof(TException)) {
            throw ThrowsException.ForIncorrectExceptionType(typeof(TException), lastException);
         }
      }
   }
}
