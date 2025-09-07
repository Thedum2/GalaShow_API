using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Response.Question
{
    public class ChoiceResponse
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }
    }
}
