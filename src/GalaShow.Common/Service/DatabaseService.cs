using System.Data.Common;
using GalaShow.Common.Configuration;
using GalaShow.Common.Infrastructure;
using MySql.Data.MySqlClient;

namespace GalaShow.Common.Service
{
    public sealed class DatabaseService : AsyncSingleton<DatabaseService>, IDisposable
    {
        private string? _connectionString;

        private DatabaseService() { }

        protected override async Task InitializeCoreAsync()
        {
            var cfg = new DatabaseConfig();
            var creds = await SecretsService.Instance.GetDbCredentialsAsync(cfg.SecretArn);

            var builder = new MySqlConnectionStringBuilder
            {
                Server = cfg.Server,
                Port = cfg.Port,
                Database = cfg.Database,
                UserID = creds.Username,
                Password = creds.Password,
                CharacterSet = "utf8mb4",
                SslMode = MySqlSslMode.Required,
                ConnectionTimeout = 30,
                Pooling = true
            };
            _connectionString = builder.ConnectionString;
            
            Console.WriteLine($"[DB] Host={builder.Server}, Port={builder.Port}, Db={builder.Database}, User={builder.UserID}, SSL={builder.SslMode}");
        }

        private MySqlConnection CreateConn()
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException("DatabaseService not initialized.");
            return new MySqlConnection(_connectionString);
        }

        public async Task<T?> ExecuteScalarAsync<T>(string sql, params MySqlParameter[] parameters)
        {
            await using var conn = CreateConn();
            await conn.OpenAsync();

            await using var cmd = new MySqlCommand(sql, conn);
            if (parameters?.Length > 0) cmd.Parameters.AddRange(parameters);

            var result = await cmd.ExecuteScalarAsync();
            return result is T t ? t : default;
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, params MySqlParameter[] parameters)
        {
            await using var conn = CreateConn();
            await conn.OpenAsync();

            await using var cmd = new MySqlCommand(sql, conn);
            if (parameters?.Length > 0) cmd.Parameters.AddRange(parameters);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<DbDataReader> ExecuteReaderAsync(string sql, params MySqlParameter[] parameters)
        {
            var conn = CreateConn();
            await conn.OpenAsync();

            var cmd = new MySqlCommand(sql, conn);
            if (parameters?.Length > 0) cmd.Parameters.AddRange(parameters);

            return await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.CloseConnection);
        }

        public override void Dispose() => base.Dispose();
    }
}
