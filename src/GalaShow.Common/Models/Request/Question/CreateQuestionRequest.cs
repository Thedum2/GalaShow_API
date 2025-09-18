using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Request.Question
{
    public class CreateQuestionRequest
    {
        [JsonPropertyName("categoryId")]
        public int CategoryId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("choices")]
        [MinLength(2)]
        [MaxLength(4)]
        public List<CreateChoiceRequest> Choices { get; set; }
    }

    public class CreateChoiceRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }
    }
}
