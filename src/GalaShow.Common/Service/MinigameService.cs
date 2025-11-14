using GalaShow.Common.Infrastructure;
using GalaShow.Common.Models.Request.Minigame;
using GalaShow.Common.Models.Response.Minigame;
using GalaShow.Common.Repositories;

namespace GalaShow.Common.Service
{
    public sealed class MinigameService : AsyncSingleton<MinigameService>
    {
        private readonly MinigameRepository _repo = new();

        private MinigameService() { }

        protected override Task InitializeCoreAsync()
        {
            return Task.CompletedTask;
        }

        // ==================== 미니게임 목록 조회 ====================
        public async Task<MinigameListResponse> GetAllMinigamesAsync(
            string? scale = null,
            string? difficulty = null,
            string? round = null,
            string? type = null,
            string? survivalRate = null,
            string? winCondition = null)
        {
            var (total, minigames) = await _repo.GetAllAsync(
                scale, difficulty, round, type, survivalRate, winCondition);

            if (minigames.Count == 0)
            {
                return new MinigameListResponse
                {
                    Total = 0,
                    Items = new List<MinigameListItemResponse>()
                };
            }

            var minigameIds = minigames.Select(m => m.Id).ToList();
            var tagsByGame = await _repo.GetTagsByMinigameIdsAsync(minigameIds);
            var tutorialsByGame = await _repo.GetTutorialsByMinigameIdsAsync(minigameIds);
            var controlsByGame = await _repo.GetControlsByMinigameIdsAsync(minigameIds);

            var items = minigames.Select(m => new MinigameListItemResponse
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                VideoUrl = m.VideoUrl,
                LogoUrl = m.LogoUrl,
                Tags = tagsByGame.ContainsKey(m.Id) ? tagsByGame[m.Id] : new MinigameTagsDto(),
                PhaseData = string.IsNullOrEmpty(m.PhaseData) ? null : System.Text.Json.JsonDocument.Parse(m.PhaseData).RootElement,
                GameData = string.IsNullOrEmpty(m.GameData) ? null : System.Text.Json.JsonDocument.Parse(m.GameData).RootElement,
                Tutorial = tutorialsByGame.ContainsKey(m.Id)
                    ? tutorialsByGame[m.Id].Select(t => new MinigameTutorialDto { Step = t.Step, Description = t.Description }).ToList()
                    : new List<MinigameTutorialDto>(),
                Controls = controlsByGame.ContainsKey(m.Id)
                    ? controlsByGame[m.Id].Select(c => new MinigameControlDto { KeyName = c.KeyName, Key = c.Keys }).ToList()
                    : new List<MinigameControlDto>(),
                CreatedAt = m.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UpdatedAt = m.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            }).ToList();

            return new MinigameListResponse
            {
                Total = total,
                Items = items
            };
        }

        // ==================== 미니게임 상세 조회 ====================
        public async Task<MinigameDetailResponse?> GetMinigameDetailAsync(int gameId)
        {
            var minigame = await _repo.GetByIdAsync(gameId);
            if (minigame == null)
                return null;

            var tagsByGame = await _repo.GetTagsByMinigameIdsAsync(new List<int> { gameId });
            var tutorials = await _repo.GetTutorialsAsync(gameId);
            var controls = await _repo.GetControlsAsync(gameId);

            return new MinigameDetailResponse
            {
                Id = minigame.Id,
                Name = minigame.Name,
                Description = minigame.Description,
                VideoUrl = minigame.VideoUrl,
                LogoUrl = minigame.LogoUrl,
                Tags = tagsByGame.ContainsKey(gameId) ? tagsByGame[gameId] : new MinigameTagsDto(),
                PhaseData = string.IsNullOrEmpty(minigame.PhaseData) ? null : System.Text.Json.JsonDocument.Parse(minigame.PhaseData).RootElement,
                GameData = string.IsNullOrEmpty(minigame.GameData) ? null : System.Text.Json.JsonDocument.Parse(minigame.GameData).RootElement,
                Tutorial = tutorials.Select(t => new MinigameTutorialDto
                {
                    Step = t.Step,
                    Description = t.Description
                }).ToList(),
                Controls = controls.Select(c => new MinigameControlDto
                {
                    KeyName = c.KeyName,
                    Key = c.Keys
                }).ToList(),
                CreatedAt = minigame.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UpdatedAt = minigame.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        // ==================== 미니게임 생성 ====================
        public async Task<MinigameCreatedResponse?> CreateMinigameAsync(CreateMinigameRequest request)
        {
            var minigameId = await _repo.CreateAsync(request);
            var created = await _repo.GetByIdAsync(minigameId);

            if (created == null)
                return null;

            return new MinigameCreatedResponse
            {
                Id = created.Id,
                Name = created.Name,
                CreatedAt = created.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UpdatedAt = created.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        // ==================== 미니게임 수정 ====================
        public async Task<MinigameUpdatedResponse?> UpdateMinigameAsync(int gameId, UpdateMinigameRequest request)
        {
            await _repo.UpdateAsync(gameId, request);
            var updated = await _repo.GetByIdAsync(gameId);

            if (updated == null)
                return null;

            return new MinigameUpdatedResponse
            {
                Id = updated.Id,
                Name = updated.Name,
                UpdatedAt = updated.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        // ==================== 미니게임 삭제 ====================
        public async Task<int> DeleteMinigameAsync(int gameId)
        {
            return await _repo.DeleteAsync(gameId);
        }

        // ==================== 검증 메서드 ====================
        public Task<bool> ExistsByIdAsync(int id) => _repo.GetByIdAsync(id).ContinueWith(t => t.Result != null);

        public Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
            => _repo.ExistsByNameAsync(name, excludeId);

        // ==================== 생존률 관련 ====================
        public async Task<SurvivalRateResponse?> GetSurvivalRateAsync(int gameId)
        {
            var minigame = await _repo.GetByIdAsync(gameId);
            if (minigame == null)
                return null;

            var stats = await _repo.GetSurvivalStatsAsync(gameId);
            if (stats == null)
                return null;

            return new SurvivalRateResponse
            {
                GameId = gameId,
                GameName = minigame.Name,
                SurvivalRate = stats.SurvivalRate,
                TotalGames = stats.TotalGames,
                TotalPlayers = stats.TotalPlayers,
                Survivors = stats.Survivors,
                LastUpdated = stats.LastUpdated.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        public async Task<SurvivalRateAddedResponse?> AddSurvivalRateAsync(int gameId, int totalPlayers, int survivors)
        {
            var minigame = await _repo.GetByIdAsync(gameId);
            if (minigame == null)
                return null;

            var previousStats = await _repo.GetSurvivalStatsAsync(gameId);
            var previousRate = previousStats?.SurvivalRate ?? 0;

            var updatedStats = await _repo.AddSurvivalDataAsync(gameId, totalPlayers, survivors);
            var addedRate = totalPlayers > 0 ? (decimal)survivors / totalPlayers * 100 : 0;

            return new SurvivalRateAddedResponse
            {
                GameId = gameId,
                SurvivalRate = updatedStats.SurvivalRate,
                PreviousRate = previousRate,
                TotalGames = updatedStats.TotalGames,
                TotalPlayers = updatedStats.TotalPlayers,
                Survivors = updatedStats.Survivors,
                AddedGame = new AddedGameInfo
                {
                    TotalPlayers = totalPlayers,
                    Survivors = survivors,
                    Rate = addedRate
                },
                UpdatedAt = updatedStats.LastUpdated.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        public async Task<SurvivalRateResponse?> UpdateSurvivalRateAsync(int gameId, decimal survivalRate, int totalGames, int totalPlayers, int survivors)
        {
            var minigame = await _repo.GetByIdAsync(gameId);
            if (minigame == null)
                return null;

            await _repo.UpdateSurvivalStatsAsync(gameId, survivalRate, totalGames, totalPlayers, survivors);
            var updated = await _repo.GetSurvivalStatsAsync(gameId);

            if (updated == null)
                return null;

            return new SurvivalRateResponse
            {
                GameId = gameId,
                GameName = minigame.Name,
                SurvivalRate = updated.SurvivalRate,
                TotalGames = updated.TotalGames,
                TotalPlayers = updated.TotalPlayers,
                Survivors = updated.Survivors,
                LastUpdated = updated.LastUpdated.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }
    }
}
