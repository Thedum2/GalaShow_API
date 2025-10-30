namespace GalaShow.Common.Data.Entities
{
    public class MinigameTutorial
    {
        public int Id { get; set; }
        public int MinigameId { get; set; }
        public int Step { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
