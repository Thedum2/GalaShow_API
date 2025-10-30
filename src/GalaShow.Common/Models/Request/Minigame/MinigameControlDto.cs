using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Request.Minigame
{
    public class MinigameControlDto
    {
        [JsonPropertyName("keyName")]
        public string KeyName { get; set; } = string.Empty;

        [JsonPropertyName("key")]
        public List<string> Key { get; set; } = new();
    }
}
