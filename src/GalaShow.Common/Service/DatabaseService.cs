using System.Data;
using System.Data.Common;
using GalaShow.Common.Configuration;
using MySql.Data.MySqlClient;

namespace GalaShow.Common.Service
{
    public class DatabaseService : IDisposable
    {
        private readonly MySqlConnection _connection;
        private bool _disposed = false;

        public DatabaseService(DatabaseConfig config, (string username, string password) credentials)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = config.Server,
                Port = config.Port,
                Database = config.Database,
                UserID = credentials.username,
                Password = credentials.password,
                CharacterSet = "utf8mb4",
                SslMode = MySqlSslMode.Required,
                ConnectionTimeout = 30,
                Pooling = false
            };

            _connection = new MySqlConnection(builder.ConnectionString);
        }

        public async Task OpenAsync()
        {
            if (_connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }
        }

        public async Task<T?> ExecuteScalarAsync<T>(string sql, params MySqlParameter[] parameters)
        {
            await using var cmd = new MySqlCommand(sql, _connection);
            if (parameters.Length > 0)
            {
                cmd.Parameters.AddRange(parameters);
            }
            
            var result = await cmd.ExecuteScalarAsync();
            return result is T ? (T)result : default(T);
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, params MySqlParameter[] parameters)
        {
            await using var cmd = new MySqlCommand(sql, _connection);
            if (parameters.Length > 0)
            {
                cmd.Parameters.AddRange(parameters);
            }
            
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<DbDataReader> ExecuteReaderAsync(string sql, params MySqlParameter[] parameters)
        {
            var cmd = new MySqlCommand(sql, _connection);
            if (parameters.Length > 0)
            {
                cmd.Parameters.AddRange(parameters);
            }
            
            return await cmd.ExecuteReaderAsync();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _connection?.Dispose();
                _disposed = true;
            }
        }
    }
}