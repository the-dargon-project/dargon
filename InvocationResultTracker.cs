using System;
using System.Collections.Generic;

namespace ItzWarty.Test
{
   internal class InvocationResultTracker
   {
      private readonly List<IInvocationResult> results = new List<IInvocationResult>();
      private readonly object defaultValue;
      private int progress = 0;


      public InvocationResultTracker(object defaultValue) { this.defaultValue = defaultValue; }
      public void AddResult(IInvocationResult result) { results.Add(result); }
      public IInvocationResult NextResult()
      {
         // index is either current progress or -1 if no results added
         var index = Math.Min(progress, results.Count - 1);

         // Don't increment progress past results.Count. This allows us to get the value
         // from our tracker, then set the next value and get that.
         if (progress < results.Count) {
            progress++;
         }

         if (index == -1) {
            return new InvocationReturnResult(defaultValue);
         } else {
            return results[index];
         }
      }
   }
}