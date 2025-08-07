using System.Text.Json.Serialization;

namespace GalaShow.Common.Models
{
    public class DbCredentials
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;
        
        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }
}