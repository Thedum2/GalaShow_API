using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Request.Minigame
{
    public class MinigameTagsDto
    {
        [JsonPropertyName("scale")]
        public List<string> Scale { get; set; } = new();

        [JsonPropertyName("difficulty")]
        public List<string> Difficulty { get; set; } = new();

        [JsonPropertyName("round")]
        public List<string> Round { get; set; } = new();

        [JsonPropertyName("type")]
        public List<string> Type { get; set; } = new();

        [JsonPropertyName("survivalRate")]
        public List<string> SurvivalRate { get; set; } = new();

        [JsonPropertyName("winCondition")]
        public List<string> WinCondition { get; set; } = new();
    }
}
