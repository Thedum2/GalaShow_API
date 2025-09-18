
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
            var affectedRows = 0;
            await DatabaseService.Instance.ExecuteInTransactionAsync(async (conn, transaction) =>
            {
                var questionIds = new List<int>();
                const string selectQuestionsSql = "SELECT id FROM questions WHERE category_id = @id";
                var p = new[] { new MySqlParameter("@id", MySqlDbType.Int32) { Value = id } };
                await using (var cmd = new MySqlCommand(selectQuestionsSql, conn, transaction))
                {
                    cmd.Parameters.AddRange(p);
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            questionIds.Add(reader.GetInt32("id"));
                        }
                    }
                }

                if (questionIds.Any())
                {
                    var deleteChoicesSql = $"DELETE FROM choices WHERE question_id IN ({string.Join(",", questionIds)})";
                    await DatabaseService.Instance.ExecuteNonQueryAsync(deleteChoicesSql, conn, transaction);

                    const string deleteQuestionsSql = "DELETE FROM questions WHERE category_id = @id";
                    await DatabaseService.Instance.ExecuteNonQueryAsync(deleteQuestionsSql, conn, transaction, p);
                }

                // 4. Delete the category itself
                const string deleteCategorySql = "DELETE FROM question_categories WHERE id = @id";
                affectedRows = await DatabaseService.Instance.ExecuteNonQueryAsync(deleteCategorySql, conn, transaction, p);
            });
            return affectedRows;
        }
    }
}
