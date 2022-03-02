using System;
using System.Threading;
using Dargon.Commons.Utilities;
using Dargon.Commons.VersionCounters;

namespace Dargon.Courier.StateReplicationTier.States {
   public delegate void StateViewUpdatedEvent();

   public interface IStateView : IVersionSource {
      bool IsReady { get; }
      event StateViewUpdatedEvent Updated;
   }

   /// <summary>
   /// Externally-synchronized state view.
   /// </summary>
   public interface IStateView<TState> : IStateView where TState : class, IState {
      TState State { get; }
   }

   public static class StateViewExtensions {
      public static MappedStateView<T2> Map<T1, T2>(this IStateView<T1> s, Func<T1, T2> getterFunc)
         where T1 : class, IState
         where T2 : class, IState
         => new(s, () => getterFunc(s.State));
   }
}

