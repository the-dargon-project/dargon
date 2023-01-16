using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Courier.PeeringTier;

namespace Dargon.Courier.SessionTier {
   /// <summary>
   /// Holds local state corresponding to the remote connection.
   /// Note: This session object is heavily heavily read-optimized and is incredibly slow
   /// for writes; it is backed by <see cref="Dargon.Courier.PeeringTier.PeerContext.LocalState"/>.
   /// </summary>
   public class CourierAmbientPeerContext : ThreadLocalContext<CourierAmbientPeerContext> {
      public required PeerContext PeerContext { get; init; }

      public bool TryGet<V>(object key, out V res) where V : class {
         if (PeerContext.LocalState.TryGetValue(key, out var existing)) {
            res = (V)existing;
            return true;
         } else {
            res = null;
            return false;
         }
      }

      public V GetOrThrow<V>(object key) where V : class
         => PeerContext.LocalState.TryGetValue(key, out var existing)
            ? (V)existing
            : throw new KeyNotFoundException($"{PeerContext.Identity} {key}");

      public bool TryGetElseAdd<V>(object key, Func<V> valueFactory, out V result) where V : class {
         return TryGetElseAdd(key, valueFactory, static vf => vf(), out result);
      }

      public bool TryGetElseAdd<T, V>(object key, T state, Func<T, V> valueFactory, out V result) where V : class {
         if (PeerContext.LocalState.TryGetElseAdd(
                key, (state, valueFactory),
                static (k, x) => k,
                static (k, x) => x.valueFactory(x.state),
                out var res)) {
            result = (V)res;
            return true;
         }

         result = default;
         return false;
      }

      public void AddOrThrow<V>(object key, V val) where V : class
         => PeerContext.LocalState.AddOrThrow(key, val);

      private static CourierAmbientPeerContext Create(PeerContext pc) {
         return new CourierAmbientPeerContext {
            PeerContext = pc,
         };
      }

      internal static CourierAmbientPeerContext GetSession(PeerContext pc) =>
         (CourierAmbientPeerContext)pc.LocalState.GetOrAdd<PeerContext>(
            LocalStateKey.Instance,
            pc,
            (_, pc) => Create(pc));


      private class LocalStateKey {
         public static readonly LocalStateKey Instance = new();
      }
   }

   public class SessionObjectBase<TSelf> where TSelf : SessionObjectBase<TSelf> {
      public static bool TryGetInstance(out TSelf res) => CourierAmbientPeerContext.CurrentContext.TryGet(Key.Instance, out res);
      public static TSelf GetInstance() => CourierAmbientPeerContext.CurrentContext.GetOrThrow<TSelf>(Key.Instance);
      public static void SetOrThrowInstance(TSelf val) => CourierAmbientPeerContext.CurrentContext.AddOrThrow(Key.Instance, val);

      private class Key {
         public static readonly Key Instance = new();
         public override string ToString() => typeof(TSelf).FullName;
      }
   }

   public static class SessionStatics {
      public static CourierAmbientPeerContext GetSession(this PeerContext pc) => CourierAmbientPeerContext.GetSession(pc);
   }
}
