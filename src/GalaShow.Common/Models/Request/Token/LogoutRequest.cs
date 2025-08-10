using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Request.Token
{
    public class LogoutRequest
    {
        [JsonPropertyName("refreshToken")]
        public string? RefreshToken { get; set; }
    }
}