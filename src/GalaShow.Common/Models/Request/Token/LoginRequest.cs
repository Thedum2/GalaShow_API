using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Request.Token
{
    public class LoginRequest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }
}