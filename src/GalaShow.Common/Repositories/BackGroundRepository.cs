using System.Data;
using GalaShow.Common.Models.Response.Background;
using GalaShow.Common.Service;
using MySql.Data.MySqlClient;

namespace GalaShow.Common.Repositories;

public class BackGroundRepository
{
    public async Task<List<BackgroundResponse>> GetAllAsync()
    {
        const string sql = @"SELECT id, title, `type`, file_url, created_at, updated_at FROM background ORDER BY id ASC";

        var list = new List<BackgroundResponse>();
        await using var reader = await DatabaseService.Instance.ExecuteReaderAsync(sql);

        while (await reader.ReadAsync())
        {
            list.Add(new BackgroundResponse
            {
                Id      = reader.GetInt32("id"),
                Title   = reader.GetString("title"),
                Type    = reader.GetString("type"),
                Url = reader.GetString("file_url")
            });
        }
        return list;
    }

    public async Task<int> UpdateAsync(int id, string title, string type, string fileUrl)
    {
        const string sql = @"UPDATE background SET title = @title, `type` = @type, file_url  = @file_url, updated_at = NOW() WHERE id = @id";

        var p = new[]
        {
            new MySqlParameter("@title",    MySqlDbType.VarChar){ Value = title },
            new MySqlParameter("@type",     MySqlDbType.VarChar){ Value = type },
            new MySqlParameter("@file_url", MySqlDbType.VarChar){ Value = fileUrl },
            new MySqlParameter("@id",       MySqlDbType.Int32){  Value = id }
        };
    
        return await DatabaseService.Instance.ExecuteNonQueryAsync(sql, p);
    }
}