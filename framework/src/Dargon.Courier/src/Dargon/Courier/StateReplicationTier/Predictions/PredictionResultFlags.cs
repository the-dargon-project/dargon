using System;

namespace Dargon.Courier.StateReplicationTier.Predictions {
   [Flags]
   public enum PredictionResultFlags {
      /// <summary>
      /// The default operation is to apply the delta (or do nothing if it is null)
      /// but keep the prediction in the predictions list.
      /// </summary>
      Default = 0,

      /// <summary>
      /// Abort; further predictions should not execute.
      /// </summary>
      IgnoreSuccessors = 1 << 0,

      /// <summary>
      /// Do not use. Is DropSuccessors without the IgnoreSuccessors flag.
      /// </summary>
      __InternalDropSuccessors = (1 << 1),

      /// <summary>
      /// Further predictions should be dropped from the prediction list.
      /// Implicitly includes <see cref="IgnoreSuccessors"/>
      /// </summary>
      DropSuccessors = __InternalDropSuccessors | IgnoreSuccessors,

      /// <summary>
      /// The current prediction should be removed from the prediction list.
      /// </summary>
      DropSelf = (1 << 2),

      __InternalAnyDropFlagMask = __InternalDropSuccessors | DropSelf,
   }
}