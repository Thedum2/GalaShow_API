using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Request
{
    public class UpdateBannerRequest
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}