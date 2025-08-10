using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Response.Token
{
    public class LoginResponse
    {
        public class UserPayload
        {
            [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
            [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;
        }

        public class TokenBundleResponse
        {
            [JsonPropertyName("accessToken")] public string AccessToken { get; set; } = string.Empty;
            [JsonPropertyName("expiresIn")] public int ExpiresIn { get; set; }
            [JsonPropertyName("accessExpiresAt")] public DateTime AccessExpiresAt { get; set; }
            [JsonPropertyName("refreshToken")] public string RefreshToken { get; set; } = string.Empty;
            [JsonPropertyName("refreshExpiresIn")] public int RefreshExpiresIn { get; set; }
            [JsonPropertyName("refreshExpiresAt")] public DateTime RefreshExpiresAt { get; set; }
            [JsonPropertyName("user")] public UserPayload User { get; set; } = new();
        }
    }
}