using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Request.Background
{
    public class UpdateBackgroundRequest
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }
}