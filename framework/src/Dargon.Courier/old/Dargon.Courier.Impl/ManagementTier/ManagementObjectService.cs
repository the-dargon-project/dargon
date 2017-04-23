using Castle.Core.Internal;
using Dargon.Commons;
using Dargon.Commons.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dargon.Commons.Collections;
using Dargon.Courier.AuditingTier.Utilities;
using Dargon.Courier.ManagementTier.Vox;
using static Dargon.Commons.Channels.ChannelsExtensions;

namespace Dargon.Courier.ManagementTier {
   [Guid("776C1CEC-44DE-40CA-9ED4-0F942BC3A8DC")]
   public interface IManagementObjectService {
      IEnumerable<ManagementObjectIdentifierDto> EnumerateManagementObjects();
      ManagementObjectStateDto GetManagementObjectDescription(Guid mobId);
      Task<object> InvokeManagedOperationAsync(string mobFullName, string methodName, object[] args);
      ManagementDataSetDto<T> GetManagedDataSet<T>(string mobFullName, string dataSetName);
   }

   public class ManagementObjectService : IManagementObjectService {
      private readonly MobContextContainer mobContextContainer;
      private readonly MobOperations mobOperations;

      public ManagementObjectService(MobContextContainer mobContextContainer, MobOperations mobOperations) {
         this.mobContextContainer = mobContextContainer;
         this.mobOperations = mobOperations;
      }

      public IEnumerable<ManagementObjectIdentifierDto> EnumerateManagementObjects() {
         return mobContextContainer.Enumerate().Select(context => context.IdentifierDto);
      }

      public ManagementObjectStateDto GetManagementObjectDescription(Guid mobId) {
         return mobContextContainer.Get(mobId).StateDto;
      }

      public Task<object> InvokeManagedOperationAsync(string mobFullName, string methodName, object[] args) {
         return mobOperations.InvokeManagedOperationAsync(mobFullName, methodName, args);
      }

      public ManagementDataSetDto<T> GetManagedDataSet<T>(string mobFullName, string dataSetName) {
         var mobContext = mobContextContainer.Get(mobFullName);
         var dataSet = (DataPointCircularBuffer<T>)mobContext.DataSetBuffersByAlias[dataSetName];
         var dataPoints = dataSet.ToArray();
         return new ManagementDataSetDto<T> {
            DataPoints = dataPoints
         };
      }
   }
}
