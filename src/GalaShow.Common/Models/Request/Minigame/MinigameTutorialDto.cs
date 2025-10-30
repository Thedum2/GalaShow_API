using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Request.Minigame
{
    public class MinigameTutorialDto
    {
        [JsonPropertyName("step")]
        public int Step { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }
}
