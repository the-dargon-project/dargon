using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMockito
{
   public static class NMockitoInvocationCounters
   {
      private static int invocationCounter = 0;
      private static int previousSequenceGroupCounter = -2;
      private static int currentSequenceGroupCounter = -1;

      public static int TakeNextInvocationCounter() { return invocationCounter++; }

      public static void AcceptVerificationCounterOrThrow(int counter, NMockitoOrder order)
      {
         if (order == NMockitoOrder.DontCare) {
            HandleDontCareVerify(counter);
         } else if (order == NMockitoOrder.AfterPrevious) {
            HandleAfterPreviousVerify(counter);
         } else if (order == NMockitoOrder.WithPrevious) {
            HandleWithPreviousVerify(counter);
         } else {
            throw new InvalidOperationException("Unknown orderness " + order);
         }
      }

      private static void HandleWithPreviousVerify(int counter)
      {
         if (counter < previousSequenceGroupCounter)
            throw new NMockitoOutOfSequenceException();

         currentSequenceGroupCounter = Math.Max(currentSequenceGroupCounter, counter);
      }

      private static void HandleAfterPreviousVerify(int counter) 
      {
         if (counter < currentSequenceGroupCounter)
            throw new NMockitoOutOfSequenceException();

         previousSequenceGroupCounter = currentSequenceGroupCounter;
         currentSequenceGroupCounter = counter;
      }

      private static void HandleDontCareVerify(int counter)
      {
         // we still track this so you can test for execution after a DontCare.
         currentSequenceGroupCounter = Math.Max(currentSequenceGroupCounter, counter);
      }
   }
}
