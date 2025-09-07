
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using GalaShow.Common.Data.Entities;
using GalaShow.Common.Service;
using MySql.Data.MySqlClient;

namespace GalaShow.Common.Repositories
{
    public class QuestionCategoryRepository
    {
        public async Task<List<QuestionCategory>> GetAllAsync()
        {
            const string sql = "SELECT id, name, created_at, updated_at FROM question_categories ORDER BY id ASC";
            var categories = new List<QuestionCategory>();

            await using var reader = await DatabaseService.Instance.ExecuteReaderAsync(sql);
            while (await reader.ReadAsync())
            {
                categories.Add(new QuestionCategory
                {
                    Id = reader.GetInt32("id"),
                    Name = reader.GetString("name"),
                    CreatedAt = reader.GetDateTime("created_at"),
                    UpdatedAt = reader.GetDateTime("updated_at")
                });
            }
            return categories;
        }

        public async Task<QuestionCategory?> GetByIdAsync(int id)
        {
            const string sql = "SELECT id, name, created_at, updated_at FROM question_categories WHERE id = @id";
            var p = new[] { new MySqlParameter("@id", MySqlDbType.Int32) { Value = id } };

            await using var reader = await DatabaseService.Instance.ExecuteReaderAsync(sql, p);
            if (!await reader.ReadAsync()) return null;
            
            return new QuestionCategory
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("name"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at")
            };
        }

        public async Task<int> CreateAsync(string name)
        {
            const string sql = "INSERT INTO question_categories (name, created_at, updated_at) VALUES (@name, NOW(), NOW()); SELECT LAST_INSERT_ID();";
            var p = new[] { new MySqlParameter("@name", MySqlDbType.VarChar) { Value = name } };
            var newId = await DatabaseService.Instance.ExecuteScalarAsync<ulong>(sql, p);
            return (int)newId;
        }

        public async Task<int> UpdateAsync(int id, string name)
        {
            const string sql = "UPDATE question_categories SET name = @name, updated_at = NOW() WHERE id = @id";
            var p = new[]
            {
                new MySqlParameter("@name", MySqlDbType.VarChar) { Value = name },
                new MySqlParameter("@id", MySqlDbType.Int32) { Value = id }
            };
            return await DatabaseService.Instance.ExecuteNonQueryAsync(sql, p);
        }

        public async Task<int> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM question_categories WHERE id = @id";
            var p = new[] { new MySqlParameter("@id", MySqlDbType.Int32) { Value = id } };
            return await DatabaseService.Instance.ExecuteNonQueryAsync(sql, p);
        }
    }
}
