using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Request.Question
{
    public class CreateQuestionRequest
    {
        [JsonPropertyName("category_id")]
        public int CategoryId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("choices")]
        public List<CreateChoiceRequest> Choices { get; set; }
    }

    public class CreateChoiceRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }
    }
}
