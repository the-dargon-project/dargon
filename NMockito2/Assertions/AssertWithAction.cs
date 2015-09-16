using System;

namespace NMockito2.Assertions {
   public class AssertWithAction {
      private readonly AssertionsProxy assertionsProxy;
      private readonly Action action;

      public AssertWithAction(AssertionsProxy assertionsProxy, Action action) {
         this.assertionsProxy = assertionsProxy;
         this.action = action;
      }

      public void Throws<TException>() where TException : Exception {
         assertionsProxy.AssertThrows<TException>(action);
      }
   }
}