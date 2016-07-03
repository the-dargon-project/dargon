using Dargon.Hydrous.Cache;
using Dargon.Hydrous.Store.Postgre;
using Dargon.Vox;

namespace Dargon.Hydrous {
   public class TestVoxTypes : VoxTypes {
      public TestVoxTypes() : base(100000) {
         // CacheFT [0, 100)
         Register(0, typeof(CustomEntryOperationFT.SetBox<>));
         Register(1, typeof(CustomEntryOperationFT.SetAddEntryOperation<,>));

         // Idless/fulValuePostgresOrmFT [100, 200)
         Register(100, typeof(IdlessValuePostgresOrmFT.TestDto));
         Register(101, typeof(IdfulValuePostgresOrmFT.TestDto));

         // WriteBehindFT  [200, 300)
         Register(200, typeof(WriteBehindFT.TestDto));
         Register(201, typeof(WriteBehindFT.AppendToNameOperation));
      }
   }
}