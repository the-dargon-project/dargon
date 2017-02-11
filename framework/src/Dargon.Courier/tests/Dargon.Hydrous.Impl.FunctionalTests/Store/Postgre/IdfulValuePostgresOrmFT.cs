using System;
using System.Threading.Tasks;
using Dargon.Hydrous.Impl.Store.Postgre;
using Dargon.Vox;
using NMockito;

namespace Dargon.Hydrous.Store.Postgre {
   public class IdfulValuePostgresOrmFT : NMockitoInstance {
      private readonly PostgresHitler<int, TestDto> hitler = new PostgresHitler<int, TestDto>("test", StaticTestConfiguration.PostgreConnectionString);

      public Task Setup() {
         return hitler.ClearAsync();
      }

      public async Task RunAsync() {
         await Setup().ConfigureAwait(false);

         // Test Insert
         var originalEntry = await hitler.InsertAsync(new TestDto { Name = "psquire" }).ConfigureAwait(false);
         var entry = originalEntry.DeepCloneSerializable();

         AssertNotEquals(0, entry.Key);
         AssertNotNull(entry.Value);
         AssertEquals("psquire", entry.Value.Name);
         AssertNotEquals(default(DateTime), entry.Value.Created);
         AssertNotEquals(default(DateTime), entry.Value.Updated);

         // Test invoke update by diff and get
         entry.Value.Name = "SolfKimblee";
         await hitler.UpdateByDiffAsync(originalEntry, entry).ConfigureAwait(false);

         // Test Get
         var updatedEntry = await hitler.GetAsync(originalEntry.Key).ConfigureAwait(false);
         AssertEquals(originalEntry.Key, updatedEntry.Key);
         AssertNotNull(updatedEntry.Value);
         AssertEquals("SolfKimblee", updatedEntry.Value.Name);
         AssertEquals(originalEntry.Value.Created, updatedEntry.Value.Created);
         AssertNotEquals(originalEntry.Value.Updated, updatedEntry.Value.Updated);

         // Test Put
         await hitler.PutAsync(originalEntry.Key, new TestDto { Name = "Kaminate" }).ConfigureAwait(false);
         var putEntry = await hitler.GetAsync(originalEntry.Key).ConfigureAwait(false);
         AssertEquals(originalEntry.Key, putEntry.Key);
         AssertNotNull(putEntry.Value);
         AssertEquals("Kaminate", putEntry.Value.Name);
         AssertTrue(putEntry.Value.Created > originalEntry.Value.Created);
         AssertTrue(putEntry.Value.Updated > originalEntry.Value.Updated);
      }

      [AutoSerializable]
      public class TestDto {
         public string Name { get; set; }
         public DateTime Created { get; set; }
         public DateTime Updated { get; set; }
      }
   }
}