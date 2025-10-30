using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Response.Minigame
{
    public class MinigameListResponse
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("items")]
        public List<MinigameListItemResponse> Items { get; set; } = new();
    }
}
