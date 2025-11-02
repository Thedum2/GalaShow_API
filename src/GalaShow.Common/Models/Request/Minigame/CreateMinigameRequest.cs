using System.Text.Json;
using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Request.Minigame
{
    public class CreateMinigameRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("videoUrl")]
        public string VideoUrl { get; set; } = string.Empty;

        [JsonPropertyName("logoUrl")]
        public string LogoUrl { get; set; } = string.Empty;

        [JsonPropertyName("tags")]
        public MinigameTagsDto Tags { get; set; } = new();

        [JsonPropertyName("phaseData")]
        public JsonElement? PhaseData { get; set; }

        [JsonPropertyName("gameData")]
        public JsonElement? GameData { get; set; }

        [JsonPropertyName("tutorial")]
        public List<MinigameTutorialDto> Tutorial { get; set; } = new();

        [JsonPropertyName("controls")]
        public List<MinigameControlDto> Controls { get; set; } = new();
    }
}
