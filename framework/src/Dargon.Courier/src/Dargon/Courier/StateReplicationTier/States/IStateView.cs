using System.Threading;
using Dargon.Commons.Utilities;

namespace Dargon.Courier.StateReplicationTier.States {
   public delegate void StateViewUpdatedEvent();

   /// <summary>
   /// Externally-synchronized state view.
   /// </summary>
   public interface IStateView<TState> where TState : class, IState {
      /// <summary>
      /// Must increase (or change) whenever state changes, avoiding duplicating previous values.
      /// Used to trivially detect when state changes.
      /// </summary>
      int Version { get; }
      TState State { get; }

      bool IsReady { get; }
      event StateViewUpdatedEvent Updated;
   }
}

