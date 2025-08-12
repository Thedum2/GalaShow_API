using System.Data;
using GalaShow.Common.Data.Entities;
using GalaShow.Common.Service;
using MySql.Data.MySqlClient;

namespace GalaShow.Common.Repositories
{
    public class PolicyRepository
    {
        private readonly DatabaseService _db;

        public PolicyRepository(DatabaseService db)
        {
            _db = db;
        }

        public async Task<Policy?> GetLatestAsync()
        {
            const string sql = @"SELECT id, terms_of_service_url, privacy_policy_url, created_at, updated_at FROM policies ORDER BY id DESC LIMIT 1";
            await using var reader = await _db.ExecuteReaderAsync(sql);
            if (!await reader.ReadAsync()) return null;

            return new Policy
            {
                Id = reader.GetInt32("id"),
                TermsOfServiceUrl = reader.GetString("terms_of_service_url"),
                PrivacyPolicyUrl  = reader.GetString("privacy_policy_url"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at"),
            };
        }

        /// <summary>
        /// 한 건만 유지하는 정책 테이블: 있으면 1건만 UPDATE, 없으면 INSERT
        /// </summary>
        public async Task<int> UpsertSingletonAsync(string tosUrl, string ppUrl)
        {
            const string updateSql = @"UPDATE policies SET terms_of_service_url = @tos, privacy_policy_url = @pp, updated_at = NOW() ORDER BY id ASC LIMIT 1";

            var p = new[]
            {
                new MySqlParameter("@tos", MySqlDbType.VarChar){ Value = tosUrl },
                new MySqlParameter("@pp",  MySqlDbType.VarChar){ Value = ppUrl  },
            };

            var affected = await _db.ExecuteNonQueryAsync(updateSql, p);
            if (affected > 0) return affected;

            const string insertSql = @"INSERT INTO policies (terms_of_service_url, privacy_policy_url, created_at, updated_at) VALUES (@tos, @pp, NOW(), NOW())";
            return await _db.ExecuteNonQueryAsync(insertSql, p);
        }
    }
}
