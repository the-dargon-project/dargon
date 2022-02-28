using System.Threading;
using Dargon.Commons.Utilities;

namespace Dargon.Courier.StateReplicationTier.States {
   public delegate void StateViewUpdatedEvent();

   public interface IStateView {
      /// <summary>
      /// Must increase (or change) whenever state changes, avoiding duplicating previous values.
      /// Used to trivially detect when state changes.
      /// </summary>
      int Version { get; }

      bool IsReady { get; }
      event StateViewUpdatedEvent Updated;
   }

   /// <summary>
   /// Externally-synchronized state view.
   /// </summary>
   public interface IStateView<TState> : IStateView where TState : class, IState {
      TState State { get; }
   }
}

