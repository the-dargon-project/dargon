using Dargon.Commons;
using Dargon.Commons.Collections;
using Dargon.Commons.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dargon.Hydrous.Impl.BinaryLogNamespaceThing {
   public class SlaveBinaryLogContainer {
      private readonly ConcurrentDictionary<Guid, BinaryLog> binaryLogsById = new ConcurrentDictionary<Guid, BinaryLog>();

      public void AddOrThrow(Guid id, BinaryLog binaryLog) {
         binaryLogsById.AddOrThrow(id, binaryLog);
      }

      public void RemoveOrThrow(Guid id) {
         BinaryLog throwaway;
         if (!binaryLogsById.TryRemove(id, out throwaway)) {
            throw new InvalidStateException();
         }
      }

      public BinaryLog GetOrThrow(Guid binaryLogId) {
         BinaryLog result;
         if (!binaryLogsById.TryGetValue(binaryLogId, out result)) {
            Console.WriteLine("HAVE " + binaryLogsById.Keys.Join(", "));

            throw new KeyNotFoundException($"Couldn't find binary log of id {binaryLogId}.");
         }
         return result;
      }
   }
}
