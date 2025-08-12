namespace GalaShow.Common.Data.Entities
{
    public class Policy
    {
        public int Id { get; set; }
        public string TermsOfServiceUrl { get; set; } = string.Empty;
        public string PrivacyPolicyUrl  { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}