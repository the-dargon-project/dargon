namespace Dargon.Commons.VersionCounters {
   public interface IVersionSource {
      /// <summary>
      /// Must increase (or change) whenever state changes, avoiding duplicating previous values.
      /// Used to trivially detect when state changes.
      /// </summary>
      public int Version { get; }
   }

   public class SimpleVersionSource : IVersionSource {
      public int Version { get; set; }
   }
}