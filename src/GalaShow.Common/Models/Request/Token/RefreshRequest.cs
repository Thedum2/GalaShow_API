using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Request.Token
{
    public class RefreshRequest
    {
        [JsonPropertyName("refreshToken")]
        public string? RefreshToken { get; set; }
    }
}