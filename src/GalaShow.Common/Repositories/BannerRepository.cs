using System.Data;
using GalaShow.Common.Data.Entities;
using GalaShow.Common.Service;
using MySql.Data.MySqlClient;

namespace GalaShow.Common.Repositories;

public class BannerRepository
{
    private readonly DatabaseService _databaseService;

    public BannerRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<List<Banner>> GetAllAsync()
    {
        const string sql = "SELECT id, message, `order`, created_at, updated_at FROM banners ORDER BY `order` ASC";
        var banners = new List<Banner>();

        using var reader = await _databaseService.ExecuteReaderAsync(sql);

        while (await reader.ReadAsync())
        {
            banners.Add(new Banner
            {
                Id = reader.GetInt32("id"),
                Message = reader.GetString("message"),
                Order = reader.GetInt32("order"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at")
            });
        }

        return banners;
    }

    public async Task<int> UpdateAsync(int id, string message)
    {
        const string sql = "UPDATE banners SET message = @message,updated_at = NOW() WHERE id = @id";
        var parameters = new[]
        {
            new MySqlParameter("@message", MySqlDbType.VarChar) { Value = message },
            new MySqlParameter("@id", MySqlDbType.Int32) { Value = id }
        };
        return await _databaseService.ExecuteNonQueryAsync(sql, parameters);
    }
}