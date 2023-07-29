using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Dargon.Commons.Utilities;
using Dargon.Commons.VanityAttributes;

namespace Dargon.Commons.VersionCounters {
   [ThreadSafe]
   public interface IVersionSource {
      /// <summary>
      /// Must increase (or change) whenever state changes, avoiding duplicating previous values.
      /// Used to trivially detect when state changes.
      /// </summary>
      public int Version { get; }
   }

   public class ZeroVersionSource : IVersionSource {
      public static ZeroVersionSource Instance { get; } = new();

      private ZeroVersionSource() { }
      public int Version => 0;
   }

   [ThreadSafe]
   public class SimpleVersionSource : IVersionSource {
      private int version;

      public int Version
      {
         get => Interlocked2.Read(ref version);
         set => Interlocked2.Write(ref version, value);
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      protected void IncrementVersion() => IncrementVersion<object>(null);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      protected void IncrementVersion<T>(T throwaway) {
         Interlocked.Increment(ref version);
      }
   }

   public static class PollingBackedVersionSource {
      public static PollingBackedVersionSource<T> Create<T>(Func<T> funcThreadSafe) where T : struct, IEquatable<T>
         => new(funcThreadSafe);
   }

   [ThreadSafe]
   public class PollingBackedVersionSource<T> : IVersionSource where T : struct, IEquatable<T> {
      private readonly Func<T> funcThreadSafe;
      private readonly ReaderWriterLockSlim rwls = new ReaderWriterLockSlim();
      private T? lastValue;
      private int version;

      public PollingBackedVersionSource(Func<T> funcThreadSafe) {
         this.funcThreadSafe = funcThreadSafe;
         this.lastValue = null;
      }

      public int Version => ComputeVersion();

      private int ComputeVersion() {
         var value = funcThreadSafe();

         using var guard = rwls.CreateUpgradableReaderGuard(GuardState.SimpleReader);
         if (!lastValue.Equals(value)) {
            guard.ReleaseReaderAndReacquireAsUpgradableReader();
            if (!lastValue.Equals(value)) {
               guard.UpgradeToWriterLock();
               lastValue = value;
               version++;
            }
         }

         return version;
      }
   }

   [ThreadSafe]
   public class CombinedVersionSource : IVersionSource {
      private readonly IVersionSource[] sources;

      public CombinedVersionSource(params IVersionSource[] sources) {
         this.sources = sources;
      }

      public int Version {
         get {
            var res = 0;

            foreach (var source in sources) {
               res += source.Version;
            }

            return res;
         }
      }
   }

   public class LambdaVersionSource : IVersionSource {
      private readonly Func<int> versionFunc;

      public LambdaVersionSource(Func<int> versionFunc) {
         this.versionFunc = versionFunc;
      }

      public int Version => versionFunc();
   }
}