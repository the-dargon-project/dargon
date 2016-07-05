using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Dargon.Vox;
using Dargon.Commons;
using Dargon.Commons.Collections;
using Dargon.Commons.Exceptions;
using Dargon.Courier;
using NLog;

namespace Dargon.Hydrous.Impl.Diagnostics {
   [AutoSerializable]
   public class OperationDiagnosticStateDto {
      public Guid OperationId { get; set; }
      public string Name { get; set; }
      public string Description { get; set; }
      public ConcurrentDictionary<int, string> Statuses { get; set; }
      public DateTime Created { get; set; }
      public DateTime Updated { get; set; }
      public bool IsDestroyed { get; set; }
      public string DestroyedStackTrace { get; set; }
      public ConcurrentDictionary<DateTime, string> Extras { get; set; }
   }

   public class OperationDiagnosticsTable {
      private static readonly Logger logger = null; //LogManager.GetCurrentClassLogger();
      private readonly ConcurrentDictionary<Guid, OperationDiagnosticStateDto> operationStatesById = new ConcurrentDictionary<Guid, OperationDiagnosticStateDto>();
      private readonly Identity identity;

      public OperationDiagnosticsTable(Identity identity) {
         this.identity = identity;
      }

      public bool TryCreate(Guid operationId, string name, string description) {
         return CreateHelper(operationId, name, description, false);
      }

      public void Create(Guid operationId, string name, string description) {
         CreateHelper(operationId, name, description, true);
      }

      private bool CreateHelper(Guid operationId, string name, string description, bool throwOnDuplicate) {
         if (!operationStatesById.TryAdd(
            operationId,
            new OperationDiagnosticStateDto {
               OperationId = operationId,
               Name = name,
               Description = description,
               Statuses = new ConcurrentDictionary<int, string> { [0] = "Created" },
               Created = DateTime.Now,
               Extras = new ConcurrentDictionary<DateTime, string>()
            })) {
            if (throwOnDuplicate) {
               throw new InvalidStateException("Row already existed");
            } else {
               logger?.Trace($"ODT {identity.Id.ToShortString()} existed, was creating with name={name}, desc={description}");
            }
            return false;
         } else {
            logger?.Trace($"ODT {identity.Id.ToShortString()} created with name={name}, desc={description}");
            return true;
         }
      }

      public void UpdateStatus(Guid operationId, int index, string status) {
         var state = operationStatesById[operationId];
         if (state.IsDestroyed) {
            throw new InvalidStateException("Stack Trace where destroyed: " + state.DestroyedStackTrace);
         } else {
            state.Statuses[index] = status;
            state.Updated = DateTime.Now;
            logger?.Trace($"ODT {identity.Id.ToShortString()} updated status={status}");
         }
      }

      public void Destroy(Guid operationId) {
         var diagnosticState = operationStatesById[operationId];
         diagnosticState.Statuses[0] = "Destroyed";
         diagnosticState.Updated = DateTime.Now;
         diagnosticState.IsDestroyed = true;
         diagnosticState.DestroyedStackTrace = Environment.StackTrace;

         operationStatesById.RemoveOrThrow(operationId, diagnosticState);
         logger?.Trace($"ODT {identity.Id.ToShortString()} destroyed");
      }

      public void AppendExtra(Guid operationId, string s) {
         operationStatesById[operationId].Extras[DateTime.Now] = s;
      }

      public IEnumerable<OperationDiagnosticStateDto> Enumerate() => operationStatesById.Values.OrderBy(v => v.Created);
   }
}
