using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using GalaShow.Common.Data.Entities;
using GalaShow.Common.Service;
using MySql.Data.MySqlClient;

namespace GalaShow.Common.Repositories
{
    public class QuestionRepository
    {
        public async Task<List<Question>> GetByCategoryAsync(int categoryId, int limit, bool shuffle)
        {
            var sql = $"SELECT id, category_id, title, created_at, updated_at FROM questions WHERE category_id = @categoryId ORDER BY {(shuffle ? "RAND()" : "id ASC")} LIMIT {limit}";
            var questions = new List<Question>();
            var p = new[] { new MySqlParameter("@categoryId", MySqlDbType.Int32) { Value = categoryId } };

            await using var reader = await DatabaseService.Instance.ExecuteReaderAsync(sql, p);
            while (await reader.ReadAsync())
            {
                questions.Add(new Question
                {
                    Id = reader.GetInt32("id"),
                    CategoryId = reader.GetInt32("category_id"),
                    Title = reader.GetString("title"),
                    CreatedAt = reader.GetDateTime("created_at"),
                    UpdatedAt = reader.GetDateTime("updated_at")
                });
            }
            return questions;
        }

        public async Task<Question?> GetByIdAsync(int id)
        {
            const string sql = "SELECT id, category_id, title, created_at, updated_at FROM questions WHERE id = @id";
            var p = new[] { new MySqlParameter("@id", MySqlDbType.Int32) { Value = id } };

            await using var reader = await DatabaseService.Instance.ExecuteReaderAsync(sql, p);
            if (await reader.ReadAsync())
            {
                return new Question
                {
                    Id = reader.GetInt32("id"),
                    CategoryId = reader.GetInt32("category_id"),
                    Title = reader.GetString("title"),
                    CreatedAt = reader.GetDateTime("created_at"),
                    UpdatedAt = reader.GetDateTime("updated_at")
                };
            }
            return null;
        }
        
        public async Task<List<Question>> GetRandomAsync(int count)
        {
            var sql = $"SELECT id, category_id, title, created_at, updated_at FROM questions ORDER BY RAND() LIMIT {count}";
            var questions = new List<Question>();

            await using var reader = await DatabaseService.Instance.ExecuteReaderAsync(sql);
            while (await reader.ReadAsync())
            {
                questions.Add(new Question
                {
                    Id = reader.GetInt32("id"),
                    CategoryId = reader.GetInt32("category_id"),
                    Title = reader.GetString("title"),
                    CreatedAt = reader.GetDateTime("created_at"),
                    UpdatedAt = reader.GetDateTime("updated_at")
                });
            }
            return questions;
        }

        public async Task<List<Choice>> GetChoicesByQuestionIdAsync(int questionId)
        {
            const string sql = "SELECT id, question_id, text, image_url FROM choices WHERE question_id = @questionId ORDER BY id ASC";
            var choices = new List<Choice>();
            var p = new[] { new MySqlParameter("@questionId", MySqlDbType.Int32) { Value = questionId } };

            await using var reader = await DatabaseService.Instance.ExecuteReaderAsync(sql, p);
            while (await reader.ReadAsync())
            {
                choices.Add(new Choice
                {
                    Id = reader.GetInt32("id"),
                    QuestionId = reader.GetInt32("question_id"),
                    Text = reader.GetString("text"),
                    ImageUrl = reader.IsDBNull("image_url") ? null : reader.GetString("image_url")
                });
            }
            return choices;
        }

        public async Task<int> CreateQuestionAsync(Question question, List<Choice> choices)
        {
            await DatabaseService.Instance.ExecuteInTransactionAsync(async (conn, transaction) =>
            {
                var questionSql = "INSERT INTO questions (category_id, title, created_at, updated_at) VALUES (@categoryId, @title, NOW(), NOW()); SELECT LAST_INSERT_ID();";
                var questionParams = new[]
                {
                    new MySqlParameter("@categoryId", MySqlDbType.Int32) { Value = question.CategoryId },
                    new MySqlParameter("@title", MySqlDbType.VarChar) { Value = question.Title }
                };

                var questionId = await DatabaseService.Instance.ExecuteScalarAsync<ulong>(questionSql, conn, transaction, questionParams);
                question.Id = (int)questionId;

                if (choices != null && choices.Any())
                {
                    await CreateChoicesAsync(question.Id, choices, conn, transaction);
                }
            });

            return question.Id;
        }

        public async Task<int> UpdateQuestionAsync(Question question, List<Choice> choices)
        {
            var affectedRows = 0;
            await DatabaseService.Instance.ExecuteInTransactionAsync(async (conn, transaction) =>
            {
                var questionSql = "UPDATE questions SET title = @title, updated_at = NOW() WHERE id = @id";
                var questionParams = new[]
                {
                    new MySqlParameter("@title", MySqlDbType.VarChar) { Value = question.Title },
                    new MySqlParameter("@id", MySqlDbType.Int32) { Value = question.Id }
                };
                affectedRows = await DatabaseService.Instance.ExecuteNonQueryAsync(questionSql, conn, transaction, questionParams);

                await DeleteChoicesByQuestionIdAsync(question.Id, conn, transaction);
                
                if (choices != null && choices.Any())
                {
                    await CreateChoicesAsync(question.Id, choices, conn, transaction);
                }
            });
            return affectedRows;
        }

        private async Task CreateChoicesAsync(int questionId, List<Choice> choices, MySqlConnection conn, MySqlTransaction tr)
        {
            var choiceSql = "INSERT INTO choices (question_id, text, image_url) VALUES (@questionId, @text, @imageUrl)";
            foreach (var choice in choices)
            {
                var choiceParams = new[]
                {
                    new MySqlParameter("@questionId", MySqlDbType.Int32) { Value = questionId },
                    new MySqlParameter("@text", MySqlDbType.VarChar) { Value = choice.Text },
                    new MySqlParameter("@imageUrl", MySqlDbType.VarChar) { Value = (object)choice.ImageUrl ?? System.DBNull.Value }
                };
                await DatabaseService.Instance.ExecuteNonQueryAsync(choiceSql, conn, tr, choiceParams);
            }
        }

        private async Task DeleteChoicesByQuestionIdAsync(int questionId, MySqlConnection conn, MySqlTransaction tr)
        {
            const string sql = "DELETE FROM choices WHERE question_id = @questionId";
            var p = new[] { new MySqlParameter("@questionId", MySqlDbType.Int32) { Value = questionId } };
            await DatabaseService.Instance.ExecuteNonQueryAsync(sql, conn, tr, p);
        }

        public async Task<int> DeleteAsync(int id)
        {
            var affectedRows = 0;
            await DatabaseService.Instance.ExecuteInTransactionAsync(async (conn, transaction) =>
            {
                await DeleteChoicesByQuestionIdAsync(id, conn, transaction);

                const string sql = "DELETE FROM questions WHERE id = @id";
                var p = new[] { new MySqlParameter("@id", MySqlDbType.Int32) { Value = id } };
                affectedRows = await DatabaseService.Instance.ExecuteNonQueryAsync(sql, conn, transaction, p);
            });
            return affectedRows;
        }
    }
}