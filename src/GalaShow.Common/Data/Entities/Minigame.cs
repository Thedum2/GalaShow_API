namespace GalaShow.Common.Data.Entities
{
    public class Minigame
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public string PhaseData { get; set; } = string.Empty;
        public string GameData { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
