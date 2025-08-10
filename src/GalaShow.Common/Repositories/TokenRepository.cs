using System.Data;
using GalaShow.Common.Data.Entities;
using GalaShow.Common.Service;
using MySql.Data.MySqlClient;

namespace GalaShow.Common.Repositories
{
    public sealed class TokenRepository
    {
        public TokenRepository() {}

        public async Task InsertAsync(string userId, string tokenHash, DateTime expiresAt, string? userAgent, string? ipAddress)
        {
            const string sql = @"INSERT INTO refresh_tokens (user_id, token_hash, expires_at, user_agent, ip_address) VALUES (@user_id, @token_hash, @expires_at, @user_agent, @ip_address);";

            var p = new[]
            {
                new MySqlParameter("@user_id",    MySqlDbType.VarChar)  { Value = userId },
                new MySqlParameter("@token_hash", MySqlDbType.VarChar)  { Value = tokenHash },
                new MySqlParameter("@expires_at", MySqlDbType.DateTime) { Value = expiresAt },
                new MySqlParameter("@user_agent", MySqlDbType.VarChar)  { Value = (object?)userAgent  ?? DBNull.Value },
                new MySqlParameter("@ip_address", MySqlDbType.VarChar)  { Value = (object?)ipAddress  ?? DBNull.Value }
            };

            await DatabaseService.Instance.ExecuteNonQueryAsync(sql, p);
        }

        public async Task<RefreshToken?> GetByHashAsync(string tokenHash)
        {
            const string sql = @"SELECT id, user_id, token_hash, created_at, expires_at, revoked_at, user_agent, ip_address FROM refresh_tokens WHERE token_hash = @token_hash LIMIT 1;";

            await using var reader = await DatabaseService.Instance.ExecuteReaderAsync(sql, new MySqlParameter("@token_hash", MySqlDbType.VarChar) { Value = tokenHash });

            if (await reader.ReadAsync())
            {
                return new RefreshToken
                {
                    Id         = reader.GetInt64("id"),
                    UserId     = reader.GetString("user_id"),
                    TokenHash  = reader.GetString("token_hash"),
                    CreatedAt  = reader.GetDateTime("created_at"),
                    ExpiresAt  = reader.GetDateTime("expires_at"),
                    RevokedAt  = reader.IsDBNull(reader.GetOrdinal("revoked_at")) ? (DateTime?)null : reader.GetDateTime("revoked_at"),
                    UserAgent  = reader.IsDBNull(reader.GetOrdinal("user_agent")) ? null : reader.GetString("user_agent"),
                    IpAddress  = reader.IsDBNull(reader.GetOrdinal("ip_address")) ? null : reader.GetString("ip_address")
                };
            }
            return null;
        }

        public async Task RevokeAsync(string tokenHash)
        {
            const string sql = @"UPDATE refresh_tokens SET revoked_at = NOW() WHERE token_hash = @token_hash;";
            await DatabaseService.Instance.ExecuteNonQueryAsync(sql, new MySqlParameter("@token_hash", MySqlDbType.VarChar) { Value = tokenHash });
        }
    }
}
