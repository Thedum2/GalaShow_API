using System.Data;
using GalaShow.Common.Data.Entities;
using GalaShow.Common.Service;
using MySql.Data.MySqlClient;

namespace GalaShow.Common.Repositories
{
    public class SnsLinkRepository
    {
        private readonly DatabaseService _db;

        public SnsLinkRepository(DatabaseService db)
        {
            _db = db;
        }

        public async Task<List<SnsLink>> GetAllAsync()
        {
            const string sql = @"
                SELECT id, title, url, icon_url, `order`, created_at, updated_at
                FROM sns_links
                ORDER BY `order` ASC, id ASC;";
            var list = new List<SnsLink>();
            await using var reader = await _db.ExecuteReaderAsync(sql);
            while (await reader.ReadAsync())
            {
                list.Add(new SnsLink
                {
                    Id = reader.GetInt32("id"),
                    Title = reader.GetString("title"),
                    Url = reader.GetString("url"),
                    IconUrl = reader.GetString("icon_url"),
                    Order = reader.GetInt32("order"),
                    CreatedAt = reader.GetDateTime("created_at"),
                    UpdatedAt = reader.GetDateTime("updated_at")
                });
            }
            return list;
        }

        /// <summary>
        /// 전달받은 목록으로 전량 교체 (DELETE ALL → INSERT ...) 방식.
        /// 트랜잭션이 필요하면 DatabaseService에 맞춰 확장해도 됨.
        /// </summary>
        public async Task<int> ReplaceAllAsync(IEnumerable<SnsLink> items)
        {
            // 1) 모두 삭제
            const string deleteSql = "DELETE FROM sns_links;";
            await _db.ExecuteNonQueryAsync(deleteSql);

            // 2) 다시 삽입
            const string insertSql = @"
                INSERT INTO sns_links (title, url, icon_url, `order`, created_at, updated_at)
                VALUES (@title, @url, @icon_url, @order, NOW(), NOW());";

            int affected = 0;
            foreach (var s in items)
            {
                var p = new[]
                {
                    new MySqlParameter("@title", MySqlDbType.VarChar){ Value = s.Title },
                    new MySqlParameter("@url",   MySqlDbType.Text)   { Value = s.Url   },
                    new MySqlParameter("@icon_url",  MySqlDbType.Text)   { Value = s.IconUrl  },
                    new MySqlParameter("@order", MySqlDbType.Int32)  { Value = s.Order },
                };
                affected += await _db.ExecuteNonQueryAsync(insertSql, p);
            }
            return affected;
        }
    }
}
