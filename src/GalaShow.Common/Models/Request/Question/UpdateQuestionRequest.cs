using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Request.Question
{
    public class UpdateQuestionRequest
    {
        [JsonPropertyName("category_id")]
        public int CategoryId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("choices")]
        public List<UpdateChoiceRequest> Choices { get; set; }
    }

    public class UpdateChoiceRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }
    }
}
