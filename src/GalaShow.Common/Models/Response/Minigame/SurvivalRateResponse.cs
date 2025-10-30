using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Response.Minigame
{
    public class SurvivalRateResponse
    {
        [JsonPropertyName("gameId")]
        public int GameId { get; set; }

        [JsonPropertyName("gameName")]
        public string GameName { get; set; } = string.Empty;

        [JsonPropertyName("survivalRate")]
        public decimal SurvivalRate { get; set; }

        [JsonPropertyName("totalGames")]
        public int TotalGames { get; set; }

        [JsonPropertyName("totalPlayers")]
        public int TotalPlayers { get; set; }

        [JsonPropertyName("survivors")]
        public int Survivors { get; set; }

        [JsonPropertyName("lastUpdated")]
        public string LastUpdated { get; set; } = string.Empty;
    }
}
