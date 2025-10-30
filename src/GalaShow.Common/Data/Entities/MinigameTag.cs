namespace GalaShow.Common.Data.Entities
{
    public class MinigameTag
    {
        public int Id { get; set; }
        public int MinigameId { get; set; }
        public string TagType { get; set; } = string.Empty;
        public string TagValue { get; set; } = string.Empty;
    }
}
