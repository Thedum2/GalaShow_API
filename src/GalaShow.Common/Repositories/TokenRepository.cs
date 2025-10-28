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
                var idOrdinal = reader.GetOrdinal("id");
                var userIdOrdinal = reader.GetOrdinal("user_id");
                var tokenHashOrdinal = reader.GetOrdinal("token_hash");
                var createdAtOrdinal = reader.GetOrdinal("created_at");
                var expiresAtOrdinal = reader.GetOrdinal("expires_at");
                var revokedAtOrdinal = reader.GetOrdinal("revoked_at");
                var userAgentOrdinal = reader.GetOrdinal("user_agent");
                var ipAddressOrdinal = reader.GetOrdinal("ip_address");

                // 필수 필드 NULL 체크
                if (reader.IsDBNull(idOrdinal) || reader.IsDBNull(userIdOrdinal) ||
                    reader.IsDBNull(tokenHashOrdinal) || reader.IsDBNull(createdAtOrdinal) ||
                    reader.IsDBNull(expiresAtOrdinal))
                {
                    Console.WriteLine($"[TokenRepository] Invalid token data - required fields are NULL. Hash: {tokenHash}");
                    return null;
                }

                return new RefreshToken
                {
                    Id         = reader.GetInt64(idOrdinal),
                    UserId     = reader.GetString(userIdOrdinal),
                    TokenHash  = reader.GetString(tokenHashOrdinal),
                    CreatedAt  = reader.GetDateTime(createdAtOrdinal),
                    ExpiresAt  = reader.GetDateTime(expiresAtOrdinal),
                    RevokedAt  = reader.IsDBNull(revokedAtOrdinal) ? (DateTime?)null : reader.GetDateTime(revokedAtOrdinal),
                    UserAgent  = reader.IsDBNull(userAgentOrdinal) ? null : reader.GetString(userAgentOrdinal),
                    IpAddress  = reader.IsDBNull(ipAddressOrdinal) ? null : reader.GetString(ipAddressOrdinal)
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
