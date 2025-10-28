using System.Data;
using GalaShow.Common.Data.Entities;
using GalaShow.Common.Service;
using MySql.Data.MySqlClient;

namespace GalaShow.Common.Repositories
{
    public class SnsLinkRepository
    {
        public async Task<List<SnsLink>> GetAllAsync()
        {
            const string sql = @"
                SELECT id, title, url, icon_url, `order`, created_at, updated_at
                FROM sns_links
                ORDER BY `order` ASC, id ASC;";
            var list = new List<SnsLink>();
            await using var reader = await DatabaseService.Instance.ExecuteReaderAsync(sql);
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

        public async Task<int> ReplaceAllAsync(IEnumerable<SnsLink> items)
        {
            const string deleteSql = "DELETE FROM sns_links;";
            await DatabaseService.Instance.ExecuteNonQueryAsync(deleteSql);

            const string insertSql = @"
                INSERT INTO sns_links (id, title, url, icon_url, `order`, created_at, updated_at)
                VALUES (@id, @title, @url, @icon_url, @order, NOW(), NOW());";

            int affected = 0;
            foreach (var s in items)
            {
                var p = new[]
                {
                    new MySqlParameter("@id", MySqlDbType.Int32){ Value = s.Id },
                    new MySqlParameter("@title", MySqlDbType.VarChar){ Value = s.Title },
                    new MySqlParameter("@url",   MySqlDbType.Text)   { Value = s.Url   },
                    new MySqlParameter("@icon_url",  MySqlDbType.Text)   { Value = s.IconUrl  },
                    new MySqlParameter("@order", MySqlDbType.Int32)  { Value = s.Order },
                };
                affected += await DatabaseService.Instance.ExecuteNonQueryAsync(insertSql, p);
            }
            return affected;
        }
    }
}
