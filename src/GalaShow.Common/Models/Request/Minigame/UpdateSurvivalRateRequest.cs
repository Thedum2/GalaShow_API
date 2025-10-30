using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Request.Minigame
{
    public class UpdateSurvivalRateRequest
    {
        [JsonPropertyName("survivalRate")]
        public decimal SurvivalRate { get; set; }

        [JsonPropertyName("totalGames")]
        public int TotalGames { get; set; }

        [JsonPropertyName("totalPlayers")]
        public int TotalPlayers { get; set; }

        [JsonPropertyName("survivors")]
        public int Survivors { get; set; }
    }
}
