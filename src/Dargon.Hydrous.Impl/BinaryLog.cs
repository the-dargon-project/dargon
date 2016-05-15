using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Hydrous.Impl {
   public class BinaryLog {
      private const string kDirectoryName = "binary_log";

      public BinaryLog() {

      }

      public void Append() {

      }

      public static BinaryLog Create(string cacheDataPath) {
         var blogPath = Path.Combine(cacheDataPath, kDirectoryName);

      }

      public static void Prune(string cacheDataPath) {

      }
   }

   public class BinaryLogFile {

   }
}
