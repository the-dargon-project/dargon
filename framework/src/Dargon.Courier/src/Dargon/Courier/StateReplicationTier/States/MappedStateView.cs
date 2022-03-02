using System;

namespace Dargon.Courier.StateReplicationTier.States {
   public class MappedStateView<TState> : IStateView<TState> where TState : class, IState {
      private readonly IStateView baseState;
      private readonly Func<TState> getterFunc;

      public MappedStateView(IStateView baseState, Func<TState> getterFunc) {
         this.baseState = baseState;
         this.getterFunc = getterFunc;
      }


      public int Version => baseState.Version;
      public bool IsReady => baseState.IsReady;

      public event StateViewUpdatedEvent Updated {
         add { baseState.Updated += value; }
         remove { baseState.Updated -= value; }
      }

      public TState State => getterFunc();
   }
}