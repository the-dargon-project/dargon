using System;
using System.Collections.Generic;

namespace NMockito
{
   internal class InvocationResultTracker
   {
      private readonly List<IInvocationExecutor> results = new List<IInvocationExecutor>();
      private readonly object defaultValue;
      private readonly List<KeyValuePair<int, object>> refReplacementsByIndex;
      private int progress = 0;


      public InvocationResultTracker(object defaultValue, List<KeyValuePair<int, object>> refReplacementsByIndex) {
         this.defaultValue = defaultValue;
         this.refReplacementsByIndex = refReplacementsByIndex;
      }

      public List<KeyValuePair<int, object>> RefReplacementsByIndex { get { return refReplacementsByIndex; } }

      public void AddResult(IInvocationExecutor executor) { results.Add(executor); }
      public IInvocationExecutor NextResult()
      {
         // index is either current progress or -1 if no results added
         var index = Math.Min(progress, results.Count - 1);

         // Don't increment progress past results.Count. This allows us to get the value
         // from our tracker, then set the next value and get that.
         if (progress < results.Count) {
            progress++;
         }

         if (index == -1) {
            return new InvocationReturnExecutor(defaultValue);
         } else {
            return results[index];
         }
      }
   }
}