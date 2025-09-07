using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Response.Question
{
    public class SimpleQuestionResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }
    }
}
