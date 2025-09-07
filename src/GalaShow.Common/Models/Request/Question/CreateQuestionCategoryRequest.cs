using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Request.Question
{
    public class CreateQuestionCategoryRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
