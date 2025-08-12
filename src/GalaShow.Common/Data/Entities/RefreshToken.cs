namespace GalaShow.Common.Data.Entities
{
    public class RefreshToken
    {
        public long Id { get; set; }
        public string UserId { get; set; } = string.Empty;   // PII 아님
        public string TokenHash { get; set; } = string.Empty; // SHA-256 hex
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? UserAgent { get; set; }
        public string? IpAddress { get; set; }
    }
}