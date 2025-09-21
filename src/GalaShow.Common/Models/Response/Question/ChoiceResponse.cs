using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Response.Question
{
    public class ChoiceResponse
    {
        [JsonPropertyName("choiceId")]
        public int ChoiceId { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }
    }
}
