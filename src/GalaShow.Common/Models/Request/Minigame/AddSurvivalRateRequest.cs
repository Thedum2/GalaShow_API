using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Request.Minigame
{
    public class AddSurvivalRateRequest
    {
        [JsonPropertyName("totalPlayers")]
        public int TotalPlayers { get; set; }

        [JsonPropertyName("survivors")]
        public int Survivors { get; set; }
    }
}
