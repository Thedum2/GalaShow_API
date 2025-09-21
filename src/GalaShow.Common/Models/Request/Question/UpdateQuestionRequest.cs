using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Request.Question
{
    public class UpdateQuestionRequest
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("choices")]
        [MinLength(2)]
        [MaxLength(4)]
        public List<UpdateChoiceRequest> Choices { get; set; }
    }

    public class UpdateChoiceRequest
    {
        [JsonPropertyName("choiceId")]
        public int ChoiceId { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }
    }
}
