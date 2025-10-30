namespace GalaShow.Common.Data.Entities
{
    public class ViewerAvatar
    {
        public int Id { get; set; }
        public int Order { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GifUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
