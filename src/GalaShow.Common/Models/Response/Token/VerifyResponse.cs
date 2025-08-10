using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Response.Token
{
    public class VerifyResponse
    {
        [JsonPropertyName("sub")] public string? Sub { get; set; }
        [JsonPropertyName("role")] public string? Role { get; set; }
        [JsonPropertyName("exp")] public string? Exp { get; set; }
        [JsonPropertyName("valid")] public bool Valid { get; set; }
    }
}