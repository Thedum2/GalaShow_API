namespace GalaShow.Common.Data.Entities
{
    public class MinigameControl
    {
        public int Id { get; set; }
        public int MinigameId { get; set; }
        public string KeyName { get; set; } = string.Empty;
        public List<string> Keys { get; set; } = new();
    }
}
