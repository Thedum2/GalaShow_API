namespace GalaShow.Common.Data.Entities
{
    public class Banner
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}