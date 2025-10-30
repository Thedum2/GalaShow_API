using System.Text.Json.Serialization;
using GalaShow.Common.Models.Request.Minigame;

namespace GalaShow.Common.Models.Response.Minigame
{
    public class MinigameListItemResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

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

        [JsonPropertyName("createdAt")]
        public string CreatedAt { get; set; } = string.Empty;

        [JsonPropertyName("updatedAt")]
        public string UpdatedAt { get; set; } = string.Empty;
    }
}
