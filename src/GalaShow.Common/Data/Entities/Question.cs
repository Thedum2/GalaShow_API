using System;

namespace GalaShow.Common.Data.Entities
{
    public class Question
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
