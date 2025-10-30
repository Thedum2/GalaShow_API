namespace GalaShow.Common.Data.Entities
{
    public class MinigameSurvivalStats
    {
        public int Id { get; set; }
        public int MinigameId { get; set; }
        public decimal SurvivalRate { get; set; }
        public int TotalGames { get; set; }
        public int TotalPlayers { get; set; }
        public int Survivors { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
