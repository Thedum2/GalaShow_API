using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Response.Minigame
{
    public class SurvivalRateAddedResponse
    {
        [JsonPropertyName("gameId")]
        public int GameId { get; set; }

        [JsonPropertyName("survivalRate")]
        public decimal SurvivalRate { get; set; }

        [JsonPropertyName("previousRate")]
        public decimal PreviousRate { get; set; }

        [JsonPropertyName("totalGames")]
        public int TotalGames { get; set; }

        [JsonPropertyName("totalPlayers")]
        public int TotalPlayers { get; set; }

        [JsonPropertyName("survivors")]
        public int Survivors { get; set; }

        [JsonPropertyName("addedGame")]
        public AddedGameInfo AddedGame { get; set; } = new();

        [JsonPropertyName("updatedAt")]
        public string UpdatedAt { get; set; } = string.Empty;
    }

    public class AddedGameInfo
    {
        [JsonPropertyName("totalPlayers")]
        public int TotalPlayers { get; set; }

        [JsonPropertyName("survivors")]
        public int Survivors { get; set; }

        [JsonPropertyName("rate")]
        public decimal Rate { get; set; }
    }
}
