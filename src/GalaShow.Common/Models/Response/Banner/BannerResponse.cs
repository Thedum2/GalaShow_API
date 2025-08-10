using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Response.Banner
{
    public class BannerResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("order")]
        public int Order { get; set; }
    }
}