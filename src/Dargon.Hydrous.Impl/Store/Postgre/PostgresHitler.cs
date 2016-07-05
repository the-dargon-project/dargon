using Dargon.Commons;
using Dargon.Commons.Exceptions;
using Dargon.Courier;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NLog;

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
            var insertedColumnNames = new List<string>();

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
                  return Entry<K, V>.Create(key);
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
            var updatedColumnNames = new List<string>();

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


      public Task PutAsync(K key, V value) {
         return ExecCommandAsync(async cmd => {
            // Retrieve all rows
            string commandBegin = $"INSERT INTO {tableName} (";
            string commandLeftMiddle = ") VALUES (";
            string commandRightMiddle = ") ON CONFLICT (id) DO UPDATE SET (";
            string commandRightRightMidle = ") = (";
            string commandEnd = $") WHERE {tableName}.id=@id";
            var updatedColumnNames = new List<string>();

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
                  conn.Open();
                  using (var cmd = new NpgsqlCommand()) {
                     cmd.Connection = conn;
                     return await callback(cmd).ConfigureAwait(false);
                  }
               }
            } catch (PostgresException e) {
               logger.Error("Postgres error: ", e);
            }
         }
      }

      private Entry<K, V> ReadToEntry(DbDataReader reader) {
         var key = (K)reader["id"];
         var value = Activator.CreateInstance<V>();
         foreach (var property in typeof(V).GetProperties()) {
            property.SetValue(value, reader[property.Name.ToLower()]);
         }
         return Entry<K, V>.HACK__Create(key, value);
      }
   }
}