using System.Data;
using GalaShow.Common.Data.Entities;
using GalaShow.Common.Models.Request.Minigame;
using GalaShow.Common.Service;
using MySql.Data.MySqlClient;

namespace GalaShow.Common.Repositories
{
    public sealed class ViewerAvatarRepository
    {
        public async Task<List<ViewerAvatar>> GetAllAsync()
        {
            const string sql = @"
                SELECT id, `order`, name, gif_url, created_at, updated_at
                FROM viewer_avatars
                ORDER BY `order` ASC";

            var avatars = new List<ViewerAvatar>();
            await using var reader = await DatabaseService.Instance.ExecuteReaderAsync(sql);
            while (await reader.ReadAsync())
            {
                avatars.Add(new ViewerAvatar
                {
                    Id = reader.GetInt32("id"),
                    Order = reader.GetInt32("order"),
                    Name = reader.GetString("name"),
                    GifUrl = reader.GetString("gif_url"),
                    CreatedAt = reader.GetDateTime("created_at"),
                    UpdatedAt = reader.GetDateTime("updated_at")
                });
            }

            return avatars;
        }

        public async Task<bool> HasDuplicateOrdersAsync(List<ViewerAvatarDto> avatars)
        {
            var orders = avatars.Select(a => a.Order).ToList();
            return orders.Count != orders.Distinct().Count();
        }

        public async Task UpdateAllAsync(List<ViewerAvatarDto> avatars)
        {
            await DatabaseService.Instance.ExecuteInTransactionAsync(async (conn, tr) =>
            {
                // 1. 기존 데이터 모두 삭제
                const string deleteSql = "DELETE FROM viewer_avatars";
                await DatabaseService.Instance.ExecuteNonQueryAsync(deleteSql, conn, tr);

                // 2. 새 데이터 삽입
                foreach (var avatar in avatars)
                {
                    const string insertSql = @"
                        INSERT INTO viewer_avatars (id, `order`, name, gif_url)
                        VALUES (@id, @order, @name, @gifUrl)";

                    var p = new[]
                    {
                        new MySqlParameter("@id", MySqlDbType.Int32) { Value = avatar.Id },
                        new MySqlParameter("@order", MySqlDbType.Int32) { Value = avatar.Order },
                        new MySqlParameter("@name", MySqlDbType.VarChar) { Value = avatar.Name },
                        new MySqlParameter("@gifUrl", MySqlDbType.VarChar) { Value = avatar.GifUrl }
                    };

                    await DatabaseService.Instance.ExecuteNonQueryAsync(insertSql, conn, tr, p);
                }
            });
        }
    }
}
