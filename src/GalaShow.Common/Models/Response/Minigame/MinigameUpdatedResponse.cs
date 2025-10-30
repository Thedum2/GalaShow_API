using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Response.Minigame
{
    public class MinigameUpdatedResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("updatedAt")]
        public string UpdatedAt { get; set; } = string.Empty;
    }
}
