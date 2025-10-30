using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Request.Minigame
{
    public class UpdateViewerAvatarsRequest
    {
        [JsonPropertyName("data")]
        public List<ViewerAvatarDto> Data { get; set; } = new();
    }

    public class ViewerAvatarDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("gifUrl")]
        public string GifUrl { get; set; } = string.Empty;
    }
}
