namespace Dargon.Courier.StateReplicationTier.States {
   public static class IStateViewExtensions {
      public static IStateView<TState> AsIStateView<TState>(this IStateView<TState> self) where TState : class, IState {
         return self;
      }
   }
}