using System;

namespace NMockito2.Assertions {
   public class AssertWithAction {
      private readonly Action action;

      public AssertWithAction(Action action) {
         this.action = action;
      }

      public void Throws<TException>() where TException : Exception {
         NMockitoInstance.Instance.AssertThrows<TException>(action);
      }
   }
}