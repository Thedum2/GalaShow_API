using System.Data;
using System.Text;
using System.Text.Json;
using GalaShow.Common.Data.Entities;
using GalaShow.Common.Models.Request.Minigame;
using GalaShow.Common.Service;
using MySql.Data.MySqlClient;

namespace GalaShow.Common.Repositories
{
    public sealed class MinigameRepository
    {
        // ==================== 미니게임 목록 조회 ====================
        public async Task<(int total, List<Minigame> items)> GetAllAsync(
            string? scale = null,
            string? difficulty = null,
            string? round = null,
            string? type = null,
            string? survivalRate = null,
            string? winCondition = null)
        {
            var whereClauses = new List<string>();
            var parameters = new List<MySqlParameter>();

            // 태그 필터링 (각 태그 타입별로 AND 조건, 같은 타입 내에서는 OR 조건)
            AddTagFilter(whereClauses, parameters, "scale", scale);
            AddTagFilter(whereClauses, parameters, "difficulty", difficulty);
            AddTagFilter(whereClauses, parameters, "round", round);
            AddTagFilter(whereClauses, parameters, "type", type);
            AddTagFilter(whereClauses, parameters, "survival_rate", survivalRate);
            AddTagFilter(whereClauses, parameters, "win_condition", winCondition);

            var whereClause = whereClauses.Count > 0 ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

            // 전체 개수 조회
            var countSql = $@"
                SELECT COUNT(DISTINCT m.id)
                FROM minigames m
                LEFT JOIN minigame_tags mt ON m.id = mt.minigame_id
                {whereClause}";

            var total = await DatabaseService.Instance.ExecuteScalarAsync<long>(countSql, parameters.ToArray());

            // 목록 조회
            var sql = $@"
                SELECT DISTINCT m.id, m.name, m.description, m.video_url, m.logo_url, m.created_at, m.updated_at
                FROM minigames m
                LEFT JOIN minigame_tags mt ON m.id = mt.minigame_id
                {whereClause}
                ORDER BY m.created_at DESC";

            var minigames = new List<Minigame>();
            await using var reader = await DatabaseService.Instance.ExecuteReaderAsync(sql, parameters.ToArray());
            while (await reader.ReadAsync())
            {
                minigames.Add(new Minigame
                {
                    Id = reader.GetInt32("id"),
                    Name = reader.GetString("name"),
                    Description = reader.GetString("description"),
                    VideoUrl = reader.GetString("video_url"),
                    LogoUrl = reader.GetString("logo_url"),
                    CreatedAt = reader.GetDateTime("created_at"),
                    UpdatedAt = reader.GetDateTime("updated_at")
                });
            }

            return ((int)total, minigames);
        }

        private void AddTagFilter(List<string> whereClauses, List<MySqlParameter> parameters, string tagType, string? tagValues)
        {
            if (string.IsNullOrWhiteSpace(tagValues)) return;

            var values = tagValues.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(v => v.Trim()).ToList();
            if (values.Count == 0) return;

            var orConditions = new List<string>();
            for (int i = 0; i < values.Count; i++)
            {
                var paramName = $"@{tagType}_{i}";
                orConditions.Add($"(mt.tag_type = @{tagType}_type AND mt.tag_value = {paramName})");
                parameters.Add(new MySqlParameter(paramName, MySqlDbType.VarChar) { Value = values[i] });
            }
            parameters.Add(new MySqlParameter($"@{tagType}_type", MySqlDbType.VarChar) { Value = tagType });
            whereClauses.Add($"({string.Join(" OR ", orConditions)})");
        }

        // ==================== 미니게임 태그 조회 ====================
        public async Task<Dictionary<int, MinigameTagsDto>> GetTagsByMinigameIdsAsync(List<int> minigameIds)
        {
            if (minigameIds.Count == 0) return new Dictionary<int, MinigameTagsDto>();

            var ids = string.Join(",", minigameIds);
            var sql = $"SELECT minigame_id, tag_type, tag_value FROM minigame_tags WHERE minigame_id IN ({ids})";

            var tagsByGame = new Dictionary<int, MinigameTagsDto>();
            await using var reader = await DatabaseService.Instance.ExecuteReaderAsync(sql);
            while (await reader.ReadAsync())
            {
                var minigameId = reader.GetInt32("minigame_id");
                var tagType = reader.GetString("tag_type");
                var tagValue = reader.GetString("tag_value");

                if (!tagsByGame.ContainsKey(minigameId))
                    tagsByGame[minigameId] = new MinigameTagsDto();

                var tags = tagsByGame[minigameId];
                switch (tagType)
                {
                    case "scale": tags.Scale.Add(tagValue); break;
                    case "difficulty": tags.Difficulty.Add(tagValue); break;
                    case "round": tags.Round.Add(tagValue); break;
                    case "type": tags.Type.Add(tagValue); break;
                    case "survival_rate": tags.SurvivalRate.Add(tagValue); break;
                    case "win_condition": tags.WinCondition.Add(tagValue); break;
                }
            }

            return tagsByGame;
        }

        // ==================== 미니게임 상세 조회 ====================
        public async Task<Minigame?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT id, name, description, video_url, logo_url, created_at, updated_at
                FROM minigames
                WHERE id = @id";

            var p = new[] { new MySqlParameter("@id", MySqlDbType.Int32) { Value = id } };

            await using var reader = await DatabaseService.Instance.ExecuteReaderAsync(sql, p);
            if (await reader.ReadAsync())
            {
                return new Minigame
                {
                    Id = reader.GetInt32("id"),
                    Name = reader.GetString("name"),
                    Description = reader.GetString("description"),
                    VideoUrl = reader.GetString("video_url"),
                    LogoUrl = reader.GetString("logo_url"),
                    CreatedAt = reader.GetDateTime("created_at"),
                    UpdatedAt = reader.GetDateTime("updated_at")
                };
            }

            return null;
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            var sql = "SELECT COUNT(*) FROM minigames WHERE name = @name";
            var parameters = new List<MySqlParameter> { new MySqlParameter("@name", MySqlDbType.VarChar) { Value = name } };

            if (excludeId.HasValue)
            {
                sql += " AND id != @id";
                parameters.Add(new MySqlParameter("@id", MySqlDbType.Int32) { Value = excludeId.Value });
            }

            var count = await DatabaseService.Instance.ExecuteScalarAsync<long>(sql, parameters.ToArray());
            return count > 0;
        }

        public async Task<List<MinigameTutorial>> GetTutorialsAsync(int minigameId)
        {
            const string sql = @"
                SELECT id, minigame_id, step, description
                FROM minigame_tutorials
                WHERE minigame_id = @minigameId
                ORDER BY step ASC";

            var p = new[] { new MySqlParameter("@minigameId", MySqlDbType.Int32) { Value = minigameId } };
            var tutorials = new List<MinigameTutorial>();

            await using var reader = await DatabaseService.Instance.ExecuteReaderAsync(sql, p);
            while (await reader.ReadAsync())
            {
                tutorials.Add(new MinigameTutorial
                {
                    Id = reader.GetInt32("id"),
                    MinigameId = reader.GetInt32("minigame_id"),
                    Step = reader.GetInt32("step"),
                    Description = reader.GetString("description")
                });
            }

            return tutorials;
        }

        public async Task<List<MinigameControl>> GetControlsAsync(int minigameId)
        {
            const string sql = @"
                SELECT id, minigame_id, key_name, `keys`
                FROM minigame_controls
                WHERE minigame_id = @minigameId";

            var p = new[] { new MySqlParameter("@minigameId", MySqlDbType.Int32) { Value = minigameId } };
            var controls = new List<MinigameControl>();

            await using var reader = await DatabaseService.Instance.ExecuteReaderAsync(sql, p);
            while (await reader.ReadAsync())
            {
                var keysJson = reader.GetString("keys");
                var keys = JsonSerializer.Deserialize<List<string>>(keysJson) ?? new List<string>();

                controls.Add(new MinigameControl
                {
                    Id = reader.GetInt32("id"),
                    MinigameId = reader.GetInt32("minigame_id"),
                    KeyName = reader.GetString("key_name"),
                    Keys = keys
                });
            }

            return controls;
        }

        // ==================== 미니게임 생성 ====================
        public async Task<int> CreateAsync(CreateMinigameRequest request)
        {
            var minigameId = 0;

            await DatabaseService.Instance.ExecuteInTransactionAsync(async (conn, tr) =>
            {
                // 1. 미니게임 기본 정보 생성
                const string insertMinigame = @"
                    INSERT INTO minigames (name, description, video_url, logo_url)
                    VALUES (@name, @description, @videoUrl, @logoUrl);
                    SELECT LAST_INSERT_ID();";

                var p = new[]
                {
                    new MySqlParameter("@name", MySqlDbType.VarChar) { Value = request.Name },
                    new MySqlParameter("@description", MySqlDbType.Text) { Value = request.Description },
                    new MySqlParameter("@videoUrl", MySqlDbType.VarChar) { Value = request.VideoUrl },
                    new MySqlParameter("@logoUrl", MySqlDbType.VarChar) { Value = request.LogoUrl }
                };

                minigameId = Convert.ToInt32(await DatabaseService.Instance.ExecuteScalarAsync<object>(insertMinigame, conn, tr, p));

                // 2. 태그 저장
                await SaveTagsAsync(minigameId, request.Tags, conn, tr);

                // 3. 튜토리얼 저장
                await SaveTutorialsAsync(minigameId, request.Tutorial, conn, tr);

                // 4. 조작법 저장
                await SaveControlsAsync(minigameId, request.Controls, conn, tr);

                // 5. 생존률 통계 초기화
                const string insertStats = @"
                    INSERT INTO minigame_survival_stats (minigame_id, survival_rate, total_games, total_players, survivors)
                    VALUES (@minigameId, 0, 0, 0, 0)";
                await DatabaseService.Instance.ExecuteNonQueryAsync(insertStats, conn, tr,
                    new MySqlParameter("@minigameId", MySqlDbType.Int32) { Value = minigameId });
            });

            return minigameId;
        }

        // ==================== 미니게임 수정 ====================
        public async Task UpdateAsync(int id, UpdateMinigameRequest request)
        {
            await DatabaseService.Instance.ExecuteInTransactionAsync(async (conn, tr) =>
            {
                // 1. 기본 정보 수정
                const string updateSql = @"
                    UPDATE minigames
                    SET name = @name, description = @description, video_url = @videoUrl, logo_url = @logoUrl, updated_at = NOW()
                    WHERE id = @id";

                var p = new[]
                {
                    new MySqlParameter("@id", MySqlDbType.Int32) { Value = id },
                    new MySqlParameter("@name", MySqlDbType.VarChar) { Value = request.Name },
                    new MySqlParameter("@description", MySqlDbType.Text) { Value = request.Description },
                    new MySqlParameter("@videoUrl", MySqlDbType.VarChar) { Value = request.VideoUrl },
                    new MySqlParameter("@logoUrl", MySqlDbType.VarChar) { Value = request.LogoUrl }
                };

                await DatabaseService.Instance.ExecuteNonQueryAsync(updateSql, conn, tr, p);

                // 2. 기존 데이터 삭제
                await DatabaseService.Instance.ExecuteNonQueryAsync(
                    "DELETE FROM minigame_tags WHERE minigame_id = @id", conn, tr,
                    new MySqlParameter("@id", MySqlDbType.Int32) { Value = id });

                await DatabaseService.Instance.ExecuteNonQueryAsync(
                    "DELETE FROM minigame_tutorials WHERE minigame_id = @id", conn, tr,
                    new MySqlParameter("@id", MySqlDbType.Int32) { Value = id });

                await DatabaseService.Instance.ExecuteNonQueryAsync(
                    "DELETE FROM minigame_controls WHERE minigame_id = @id", conn, tr,
                    new MySqlParameter("@id", MySqlDbType.Int32) { Value = id });

                // 3. 새 데이터 저장
                await SaveTagsAsync(id, request.Tags, conn, tr);
                await SaveTutorialsAsync(id, request.Tutorial, conn, tr);
                await SaveControlsAsync(id, request.Controls, conn, tr);
            });
        }

        // ==================== 미니게임 삭제 ====================
        public async Task<int> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM minigames WHERE id = @id";
            var p = new[] { new MySqlParameter("@id", MySqlDbType.Int32) { Value = id } };
            return await DatabaseService.Instance.ExecuteNonQueryAsync(sql, p);
        }

        // ==================== 헬퍼 메서드 ====================
        private async Task SaveTagsAsync(int minigameId, MinigameTagsDto tags, MySqlConnection conn, MySqlTransaction tr)
        {
            var allTags = new List<(string type, string value)>();
            foreach (var scale in tags.Scale) allTags.Add(("scale", scale));
            foreach (var difficulty in tags.Difficulty) allTags.Add(("difficulty", difficulty));
            foreach (var round in tags.Round) allTags.Add(("round", round));
            foreach (var type in tags.Type) allTags.Add(("type", type));
            foreach (var survivalRate in tags.SurvivalRate) allTags.Add(("survival_rate", survivalRate));
            foreach (var winCondition in tags.WinCondition) allTags.Add(("win_condition", winCondition));

            foreach (var (type, value) in allTags)
            {
                const string sql = @"
                    INSERT INTO minigame_tags (minigame_id, tag_type, tag_value)
                    VALUES (@minigameId, @tagType, @tagValue)";

                var p = new[]
                {
                    new MySqlParameter("@minigameId", MySqlDbType.Int32) { Value = minigameId },
                    new MySqlParameter("@tagType", MySqlDbType.VarChar) { Value = type },
                    new MySqlParameter("@tagValue", MySqlDbType.VarChar) { Value = value }
                };

                await DatabaseService.Instance.ExecuteNonQueryAsync(sql, conn, tr, p);
            }
        }

        private async Task SaveTutorialsAsync(int minigameId, List<MinigameTutorialDto> tutorials, MySqlConnection conn, MySqlTransaction tr)
        {
            foreach (var tutorial in tutorials)
            {
                const string sql = @"
                    INSERT INTO minigame_tutorials (minigame_id, step, description)
                    VALUES (@minigameId, @step, @description)";

                var p = new[]
                {
                    new MySqlParameter("@minigameId", MySqlDbType.Int32) { Value = minigameId },
                    new MySqlParameter("@step", MySqlDbType.Int32) { Value = tutorial.Step },
                    new MySqlParameter("@description", MySqlDbType.Text) { Value = tutorial.Description }
                };

                await DatabaseService.Instance.ExecuteNonQueryAsync(sql, conn, tr, p);
            }
        }

        private async Task SaveControlsAsync(int minigameId, List<MinigameControlDto> controls, MySqlConnection conn, MySqlTransaction tr)
        {
            foreach (var control in controls)
            {
                const string sql = @"
                    INSERT INTO minigame_controls (minigame_id, key_name, `keys`)
                    VALUES (@minigameId, @keyName, @keys)";

                var keysJson = JsonSerializer.Serialize(control.Key);
                var p = new[]
                {
                    new MySqlParameter("@minigameId", MySqlDbType.Int32) { Value = minigameId },
                    new MySqlParameter("@keyName", MySqlDbType.VarChar) { Value = control.KeyName },
                    new MySqlParameter("@keys", MySqlDbType.JSON) { Value = keysJson }
                };

                await DatabaseService.Instance.ExecuteNonQueryAsync(sql, conn, tr, p);
            }
        }

        // ==================== 생존률 관련 ====================
        public async Task<MinigameSurvivalStats?> GetSurvivalStatsAsync(int minigameId)
        {
            const string sql = @"
                SELECT id, minigame_id, survival_rate, total_games, total_players, survivors, last_updated
                FROM minigame_survival_stats
                WHERE minigame_id = @minigameId";

            var p = new[] { new MySqlParameter("@minigameId", MySqlDbType.Int32) { Value = minigameId } };

            await using var reader = await DatabaseService.Instance.ExecuteReaderAsync(sql, p);
            if (await reader.ReadAsync())
            {
                return new MinigameSurvivalStats
                {
                    Id = reader.GetInt32("id"),
                    MinigameId = reader.GetInt32("minigame_id"),
                    SurvivalRate = reader.GetDecimal("survival_rate"),
                    TotalGames = reader.GetInt32("total_games"),
                    TotalPlayers = reader.GetInt32("total_players"),
                    Survivors = reader.GetInt32("survivors"),
                    LastUpdated = reader.GetDateTime("last_updated")
                };
            }

            return null;
        }

        public async Task<MinigameSurvivalStats> AddSurvivalDataAsync(int minigameId, int totalPlayers, int survivors)
        {
            MinigameSurvivalStats? result = null;

            await DatabaseService.Instance.ExecuteInTransactionAsync(async (conn, tr) =>
            {
                // 현재 통계 조회
                var currentStats = await GetSurvivalStatsInTransactionAsync(minigameId, conn, tr);

                var newTotalGames = currentStats.TotalGames + 1;
                var newTotalPlayers = currentStats.TotalPlayers + totalPlayers;
                var newSurvivors = currentStats.Survivors + survivors;
                var newSurvivalRate = newTotalPlayers > 0 ? (decimal)newSurvivors / newTotalPlayers * 100 : 0;

                const string sql = @"
                    UPDATE minigame_survival_stats
                    SET total_games = @totalGames, total_players = @totalPlayers, survivors = @survivors,
                        survival_rate = @survivalRate, last_updated = NOW()
                    WHERE minigame_id = @minigameId";

                var p = new[]
                {
                    new MySqlParameter("@minigameId", MySqlDbType.Int32) { Value = minigameId },
                    new MySqlParameter("@totalGames", MySqlDbType.Int32) { Value = newTotalGames },
                    new MySqlParameter("@totalPlayers", MySqlDbType.Int32) { Value = newTotalPlayers },
                    new MySqlParameter("@survivors", MySqlDbType.Int32) { Value = newSurvivors },
                    new MySqlParameter("@survivalRate", MySqlDbType.Decimal) { Value = newSurvivalRate }
                };

                await DatabaseService.Instance.ExecuteNonQueryAsync(sql, conn, tr, p);

                result = await GetSurvivalStatsInTransactionAsync(minigameId, conn, tr);
            });

            return result!;
        }

        public async Task UpdateSurvivalStatsAsync(int minigameId, decimal survivalRate, int totalGames, int totalPlayers, int survivors)
        {
            const string sql = @"
                UPDATE minigame_survival_stats
                SET survival_rate = @survivalRate, total_games = @totalGames, total_players = @totalPlayers,
                    survivors = @survivors, last_updated = NOW()
                WHERE minigame_id = @minigameId";

            var p = new[]
            {
                new MySqlParameter("@minigameId", MySqlDbType.Int32) { Value = minigameId },
                new MySqlParameter("@survivalRate", MySqlDbType.Decimal) { Value = survivalRate },
                new MySqlParameter("@totalGames", MySqlDbType.Int32) { Value = totalGames },
                new MySqlParameter("@totalPlayers", MySqlDbType.Int32) { Value = totalPlayers },
                new MySqlParameter("@survivors", MySqlDbType.Int32) { Value = survivors }
            };

            await DatabaseService.Instance.ExecuteNonQueryAsync(sql, p);
        }

        private async Task<MinigameSurvivalStats> GetSurvivalStatsInTransactionAsync(int minigameId, MySqlConnection conn, MySqlTransaction tr)
        {
            const string sql = @"
                SELECT id, minigame_id, survival_rate, total_games, total_players, survivors, last_updated
                FROM minigame_survival_stats
                WHERE minigame_id = @minigameId";

            await using var cmd = new MySqlCommand(sql, conn, tr);
            cmd.Parameters.Add(new MySqlParameter("@minigameId", MySqlDbType.Int32) { Value = minigameId });

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new MinigameSurvivalStats
                {
                    Id = reader.GetInt32("id"),
                    MinigameId = reader.GetInt32("minigame_id"),
                    SurvivalRate = reader.GetDecimal("survival_rate"),
                    TotalGames = reader.GetInt32("total_games"),
                    TotalPlayers = reader.GetInt32("total_players"),
                    Survivors = reader.GetInt32("survivors"),
                    LastUpdated = reader.GetDateTime("last_updated")
                };
            }

            throw new Exception("Survival stats not found");
        }
    }
}
