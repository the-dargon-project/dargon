using System;
using System.Security.Cryptography;
using System.Text;
using Dargon.Commons;
using Dargon.Hydrous.Impl.Store;
using Dargon.Hydrous.Impl.Store.Postgre;
using Dargon.Vox;

namespace Dargon.Hydrous.Impl {
   [AutoSerializable]
   public class CacheConfiguration<K, V> {
      public CacheConfiguration(string cacheName) {
         CacheName = cacheName;
         CacheId = ComputeCacheId(cacheName);
      }
      /// <summary>
      /// Friendly name of the cache. Preferably a valid file name.
      /// </summary>
      public string CacheName { get; private set; }

      /// <summary>
      /// Unique id based on cache name
      /// </summary>
      public Guid CacheId { get; private set; }

      public PartitioningConfiguration PartitioningConfiguration { get; set; } = new PartitioningConfiguration();
      public StaticClusterConfiguration StaticClusterConfiguration { get; set; } = new StaticClusterConfiguration();
      public ICachePersistenceStrategy<K, V> CachePersistenceStrategy { get; set; } = new NullCachePersistenceStrategy<K, V>();

      private static Guid ComputeCacheId(string cacheName) {
         var cacheNameMd5 = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(cacheName));
         return new Guid(cacheNameMd5.SubArray(0, 16));
      }
   }

   [AutoSerializable]
   public class PartitioningConfiguration {
      /// <summary>
      /// Default: 10 to get 2^10 Blocks.
      /// The top BlockCountPower bits of a key's hash determine
      /// its blockId.
      /// </summary>
      public int BlockCountPower { get; set; } = 10;

      /// <summary>
      /// Derived number of blocks
      /// </summary>
      public int DerivedBlockCount => 1 << BlockCountPower;

      /// <summary>
      /// Replication Factor.
      /// Default: 2: Entries are mirrored on two cohorts at a time.
      /// </summary>
      public int Redundancy { get; set; } = 2;
   }

   [AutoSerializable]
   public class StaticClusterConfiguration {
      /// <summary>
      /// Sleep duration while indeterminate nodes waits for leader
      /// heartbeats before entering an election phase.
      /// </summary>
      public int IndeterminateQuietPeriod { get; set; } = 5000;

      /// <summary>
      /// Initial ticks until candidate nodes declare election victory.
      /// </summary>
      public int CandidateInitialTicksToVictory { get; set; } = 5;

      /// <summary>
      /// Interval between decrements of a candidate's tick count. If the
      /// candidate's tick count reaches zero, the candidate declares election
      /// victory.
      /// </summary>
      public int CandidateTickInterval { get; set; } = 500;

      /// <summary>
      /// Maximum interval between leader heartbeats before a cohort determines
      /// that a leader node has dropped.
      /// </summary>
      public int CoordinatorTimeoutPeriod { get; set; } = 5000;

      /// <summary>
      /// Interval between coordinator heartbeats.
      /// </summary>
      public int CoordinatorHeartbeatInterval { get; set; } = 500;
   }
}