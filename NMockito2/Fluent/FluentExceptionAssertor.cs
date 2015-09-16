using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMockito2.Assertions;
using Xunit.Sdk;

namespace NMockito2.Fluent {
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
            throw new ThrowsException(typeof(TException));
         } else if (lastException.GetType() != typeof(TException)) {
            throw new ThrowsException(typeof(TException), lastException);
         }
      }
   }
}
