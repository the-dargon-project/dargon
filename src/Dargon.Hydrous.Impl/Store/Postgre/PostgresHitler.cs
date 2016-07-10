using Dargon.Commons;
using Dargon.Commons.Exceptions;
using Dargon.Courier;
using Npgsql;
using System;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Commons.Collections;
using Dargon.Commons.Comparers;
using NLog;
using SCG = System.Collections.Generic;

namespace Dargon.Hydrous.Impl.Store.Postgre {
   public class PostgresHitler<K, V> : IHitler<K, V> {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();
      private readonly string tableName;
      private readonly string connectionString;

      public PostgresHitler(string tableName, string connectionString) {
         this.tableName = tableName;
         this.connectionString = connectionString;
      }

      public Task<int> ClearAsync() {
         return ExecCommandAsync(cmd => {
            cmd.CommandText = "DELETE FROM " + tableName;
            return cmd.ExecuteNonQueryAsync();
         });
      }

      public Task<Entry<K, V>> InsertAsync(V item) {
         return ExecCommandAsync(async cmd => {
            // Retrieve all rows
            var commandStart = $"INSERT INTO {tableName} (";
            var commandMiddle = ") VALUES (";
            var commandEnd = ") RETURNING *";
            var insertedColumnNames = new SCG.List<string>();

            foreach (var property in typeof(V).GetProperties()) {
               var columnName = property.Name.ToLower();
               if (columnName == "id") {
                  continue;
               }

               var propertyValue = property.GetValue(item);
               var defaultPropertyValue = property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null;

               if (!Equals(propertyValue, defaultPropertyValue)) {
                  insertedColumnNames.Add(columnName);

                  var parameter = cmd.CreateParameter();
                  parameter.ParameterName = columnName;
                  parameter.Value = propertyValue;
                  cmd.Parameters.Add(parameter);
               }
            }
            cmd.CommandText = commandStart +
                              insertedColumnNames.Join(", ") +
                              commandMiddle +
                              insertedColumnNames.Select(c => $"@{c}").Join(", ") +
                              commandEnd;

            using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false)) {
               Trace.Assert(reader.HasRows);
               var readSuccessful = await reader.ReadAsync().ConfigureAwait(false);
               Trace.Assert(readSuccessful);

               var entry = ReadToEntry(reader);
               readSuccessful = await reader.ReadAsync().ConfigureAwait(false);
               Trace.Assert(!readSuccessful);

               return entry;
            }
         });
      }

      public Task<Entry<K, V>> GetAsync(K key) {
         return ExecCommandAsync(async cmd => {
            // Retrieve all rows
            cmd.CommandText = $"SELECT * FROM {tableName} WHERE id=@Id";
            var idParameter = cmd.CreateParameter();
            idParameter.ParameterName = "Id";
            idParameter.Value = key;
            cmd.Parameters.Add(idParameter);

            using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false)) {
               var readSuccessful = await reader.ReadAsync().ConfigureAwait(false);
               if (!readSuccessful) {
                  return Entry<K, V>.CreateNonexistant(key);
               } else {
                  var entry = ReadToEntry(reader);
                  Trace.Assert(!await reader.ReadAsync().ConfigureAwait(false));
                  return entry;
               }
            }
         });
      }

      public Task UpdateByDiffAsync(Entry<K, V> existing, Entry<K, V> updated) {
         Trace.Assert(Equals(existing.Key, updated.Key));
         return UpdateByDiffHelperAsync(existing.Key, existing.Value, updated.Value);
      }

      private Task UpdateByDiffHelperAsync(K key, V existing, V updated) {
         return ExecCommandAsync(async cmd => {
            // Retrieve all rows
            string commandBegin = $"UPDATE {tableName} SET (";
            string commandMiddle = ") = (";
            string commandEnd = ") WHERE test.id=@id";
            var updatedColumnNames = new SCG.List<string>();

            var properties = typeof(V).GetProperties();
            foreach (var p in properties) {
               var columnName = p.Name.ToLower();
               if (columnName == "updated") {
                  p.SetValue(updated, DateTime.Now);
               }

               if (object.Equals(p.GetValue(existing), p.GetValue(updated))) {
                  continue;
               }

               var propertyValue = p.GetValue(updated);

               if (columnName == "id") {
                  throw new InvalidStateException();
               } else {
                  var param = cmd.CreateParameter();
                  param.ParameterName = columnName;
                  param.Value = propertyValue ?? DBNull.Value;
                  cmd.Parameters.Add(param);
                  updatedColumnNames.Add(columnName);
               }
            }

            var idParam = cmd.CreateParameter();
            idParam.ParameterName = "id";
            idParam.Value = key;
            cmd.Parameters.Add(idParam);

            cmd.CommandText = commandBegin +
                              updatedColumnNames.Concat("id").Join(", ") +
                              commandMiddle +
                              updatedColumnNames.Concat("id").Select(x => $"@{x}").Join(", ") +
                              commandEnd;

            var rowsModified = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            Trace.Assert(1 == rowsModified);
         });
      }

      public Task BatchUpdateAsync(SCG.IReadOnlyList<PendingUpdate<K, V>> inputPendingUpdates) {
         return ExecCommandAsync(async cmd => {
            var properties = typeof(V).GetProperties();
            bool hasUpdatedColumn = false;
            var pendingUpdatesByUpdatedPropertyGroup = new MultiValueDictionary<string[], PendingUpdate<K, V>>(
               new LambdaEqualityComparer<string[]>(
                  (a, b) => a.Length == b.Length && a.Zip(b, (aElement, bElement) => aElement == bElement).All(x => x),
                  a => a.Aggregate(13, (h, x) => h * 17 + x.GetHashCode())
                  ));
            var pendingInserts = new SCG.List<PendingUpdate<K, V>>();
            foreach (var pendingUpdate in inputPendingUpdates) {
               if (!pendingUpdate.Base.Exists) {
                  pendingInserts.Add(pendingUpdate);
               } else {
                  SortedSet<string> updatedProperties = new SortedSet<string>();
                  foreach (var p in properties) {
                     var columnName = p.Name.ToLower();
                     if (columnName == "updated") {
                        hasUpdatedColumn = true;
                        continue;
                     }

                     if (object.Equals(p.GetValue(pendingUpdate.Base.Value), p.GetValue(pendingUpdate.Updated.Value))) {
                        continue;
                     }

                     if (columnName == "id") {
                        throw new InvalidStateException();
                     } else {
                        updatedProperties.Add(p.Name);
                     }
                  }
                  pendingUpdatesByUpdatedPropertyGroup.Add(updatedProperties.ToArray(), pendingUpdate);
               }
            }

            var commandTextBuilder = new StringBuilder();
            /*
             * INSERT INTO test (id, name, updated) 
             * SELECT 
             *    unnest(@ids), unnest(@names), unnest(@updateds) 
             * ON CONFLICT (id) DO UPDATE 
             * SET 
             *    name = excluded.name, updated = excluded.updated
             */
            var batchIndex = 0;
            foreach (var kvp in pendingUpdatesByUpdatedPropertyGroup) {
               var updatedPropertyNames = kvp.Key;
               var updatedProperties = updatedPropertyNames.Map(n => typeof(V).GetProperty(n));

               var updatedColumnNames = kvp.Key.Map(x => x.ToLower());
               var pendingUpdates = kvp.Value.ToArray();

               var additionalColumns = new SCG.List<string>();

               var idParameter = cmd.CreateParameter();
               idParameter.ParameterName = "id" + batchIndex;
               idParameter.Value = pendingUpdates.Map(p => p.Base.Key);
               cmd.Parameters.Add(idParameter);

               if (hasUpdatedColumn) {
                  var updatedParameter = cmd.CreateParameter();
                  updatedParameter.ParameterName = "updated" + batchIndex;
                  updatedParameter.Value = pendingUpdates.Map(p => DateTime.Now);
                  cmd.Parameters.Add(updatedParameter);
                  additionalColumns.Add("updated");
               }
               
               for (var i = 0; i < updatedPropertyNames.Length; i++) {
                  var updatedPropertyName = updatedPropertyNames[i];
                  var updatedProperty = typeof(V).GetProperty(updatedPropertyName);
                  var array =  Array.CreateInstance(updatedProperty.PropertyType, pendingUpdates.Length);
                  for (var j = 0; j < pendingUpdates.Length; j++) {
                     array.SetValue(updatedProperty.GetValue(pendingUpdates[j].Updated.Value), j);
                  }
                  var parameter = cmd.CreateParameter();
                  parameter.ParameterName = updatedColumnNames[i] + batchIndex;
                  parameter.Value = array;
                  cmd.Parameters.Add(parameter);
               }

               var query = $"UPDATE {tableName} " +
                           "SET " +
                           updatedColumnNames.Concat(additionalColumns).Select(n => $"{n} = temp.{n}").Join(", ") + " " +
                           "FROM ( select " +
                           updatedColumnNames.Concat("id").Concat(additionalColumns).Select(n => $"unnest(@{n}{batchIndex}) as {n}").Join(", ") +
                           " ) as temp " +
                           $"where {tableName}.id = temp.id";

               commandTextBuilder.Append(query);
               commandTextBuilder.Append("; ");
               batchIndex++;
            }

            // inserts;
            if (pendingInserts.Any()) {
               var propertyNames = properties.Map(p => p.Name);
               var columnNames = properties.Map(p => p.Name.ToLower());

               var idParameter = cmd.CreateParameter();
               idParameter.ParameterName = "id_ins";
               idParameter.Value = pendingInserts.Map(p => p.Base.Key);
               cmd.Parameters.Add(idParameter);

               for (var i = 0; i < properties.Length; i++) {
                  var property = properties[i];
                  var array = Array.CreateInstance(property.PropertyType, pendingInserts.Count);
                  for (var j = 0; j < pendingInserts.Count; j++) {
                     object propertyValue;
                     if (columnNames[i] == "updated" || columnNames[i] == "created") {
                        propertyValue = DateTime.Now;
                     } else {
                        propertyValue = property.GetValue(pendingInserts[j].Updated.Value);
                     }
                     array.SetValue(propertyValue, j);
                  }
                  var parameter = cmd.CreateParameter();
                  parameter.ParameterName = columnNames[i] + "_ins";
                  parameter.Value = array;
                  cmd.Parameters.Add(parameter);
               }

               var query = $"INSERT INTO {tableName} ({columnNames.Concat("id").Join(", ")}) " +
                           "SELECT " +
                           columnNames.Concat("id").Select(n => $"unnest(@{n}_ins)").Join(", ") + " " +
                           "ON CONFLICT (id) DO UPDATE " +
                           "SET " +
                           columnNames.Select(n => $"{n} = excluded.{n}").Join(", ");
               commandTextBuilder.Append(query);
               commandTextBuilder.Append("; ");
            }
            cmd.CommandText = commandTextBuilder.ToString();

            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
         });
      }

      public Task PutAsync(K key, V value) {
         return ExecCommandAsync(async cmd => {
            // Retrieve all rows
            string commandBegin = $"INSERT INTO {tableName} (";
            string commandLeftMiddle = ") VALUES (";
            string commandRightMiddle = ") ON CONFLICT (id) DO UPDATE SET (";
            string commandRightRightMidle = ") = (";
            string commandEnd = $") WHERE {tableName}.id=@id";
            var updatedColumnNames = new SCG.List<string>();

            var properties = typeof(V).GetProperties();
            foreach (var p in properties) {
               var propertyValue = p.GetValue(value);
               var columnName = p.Name.ToLower();

               if (columnName == "id") {
                  Trace.Assert(object.Equals(key, propertyValue));
               } else {
                  if (columnName == "created" || columnName == "updated") {
                     propertyValue = DateTime.Now;
                  }

                  var param = cmd.CreateParameter();
                  param.ParameterName = columnName;
                  param.Value = propertyValue ?? DBNull.Value;
                  cmd.Parameters.Add(param);
                  updatedColumnNames.Add(columnName);
               }
            }

            var idParam = cmd.CreateParameter();
            idParam.ParameterName = "id";
            idParam.Value = key;
            cmd.Parameters.Add(idParam);

            cmd.CommandText = commandBegin +
                              updatedColumnNames.Concat("id").Join(", ") +
                              commandLeftMiddle +
                              updatedColumnNames.Concat("id").Select(x => $"@{x}").Join(", ") +
                              commandRightMiddle +
                              updatedColumnNames.Join(", ") +
                              commandRightRightMidle +
                              updatedColumnNames.Select(x => $"@{x}").Join(", ") +
                              commandEnd;

            var rowsAffected = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            Trace.Assert(1 == rowsAffected);
         });
      }

      private async Task ExecCommandAsync(Func<NpgsqlCommand, Task> callback) {
         await ExecCommandAsync<object>(async cmd => {
            await callback(cmd).ConfigureAwait(false);
            return null;
         }).ConfigureAwait(false);
      }

      private async Task<TResult> ExecCommandAsync<TResult>(Func<NpgsqlCommand, Task<TResult>> callback) {
         while (true) {
            try {
               using (var conn = new NpgsqlConnection(connectionString)) {
                  await conn.OpenAsync().ConfigureAwait(false);
                  try {
                     using (var cmd = new NpgsqlCommand()) {
                        cmd.Connection = conn;
                        return await callback(cmd).ConfigureAwait(false);
                     }
                  } finally {
                     conn.Close();
                  }
               }
            } catch (PostgresException e) {
               logger.Error("Postgres error: ", e);
            } catch (NpgsqlException e) {
               logger.Error("Npgsql error: ", e);
            }
         }
      }

      private Entry<K, V> ReadToEntry(DbDataReader reader) {
         var key = (K)reader["id"];
         var value = Activator.CreateInstance<V>();
         foreach (var property in typeof(V).GetProperties()) {
            property.SetValue(value, reader[property.Name.ToLower()]);
         }
         return Entry<K, V>.CreateExistantWithValue(key, value);
      }
   }
}