using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Response.Minigame
{
    public class ViewerAvatarResponse
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
