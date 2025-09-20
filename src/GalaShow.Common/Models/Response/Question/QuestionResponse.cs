using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace GalaShow.Common.Models.Response.Question
{
    public class QuestionResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("categoryId")]
        public int CategoryId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("choices")]
        public List<ChoiceResponse> Choices { get; set; }
    }
}
