using System.ComponentModel.DataAnnotations.Schema;

namespace GalaShow.Common.Data.Entities
{
    public class Choice
    {
        public int Id { get; set; }
        public int ChoiceId { get; set; }
        public int QuestionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}
